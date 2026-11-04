# TicketsAPI

API REST para gestionar tickets de soporte (web, WhatsApp y email). .NET 10, ASP.NET Core, Entity Framework Core y SQL Server / Azure SQL.

Desplegada en Azure. Al final de este archivo están el despliegue, la consulta KQL, el uso de IA y lo pendiente. Los scripts SQL están en `db/`.

## Estructura

```
TicketsAPI/                  Proyecto de la API
  src/Controllers/           Endpoints (tickets y webhook)
  src/Services/              Reglas de negocio e idempotencia del webhook
  src/Repository/            Consultas con EF Core (AsNoTracking en lecturas)
  src/Data/                  AppDbContext (solo conexión) y Configurations/ (mapeo)
  src/Models/                Entidad Ticket y valores permitidos
  src/Dtos/                  Requests y responses con Data Annotations
  src/Common/                GlobalExceptionHandler
TicketsAPI.Tests/            Pruebas unitarias y de integración (xUnit)
```

## Ejecutar

Requisitos: .NET SDK 10 y una base SQL Server `TicketsFrefity` creada con los scripts de `db/`: `01_schema.sql` y luego `02_seed.sql`, ambos re-ejecutables (con `sqlcmd`, SSMS o el Query editor del portal).

```bash
dotnet run --project TicketsAPI/TicketsAPI.csproj --launch-profile http
```

| Recurso | URL |
| :- | :- |
| API | `http://localhost:5196` |
| Swagger UI | `http://localhost:5196/swagger` |
| OpenAPI (JSON) | `http://localhost:5196/openapi/v1.json` |
| Salud | `http://localhost:5196/health` |

En Development la cadena local está en `appsettings.Development.json` (autenticación integrada de Windows, sin contraseña). Fuera de Development (Azure) la API lee el secreto `ConnectionStrings--DefaultConnection` de Key Vault con la Managed Identity; la URI está en `KeyVault:Uri`.

## Configuración

| Clave | Para qué sirve |
| :- | :- |
| `KeyVault__Uri` | URI del Key Vault (`https://<vault>.vault.azure.net/`). Fuera de Development, si no hay cadena inyectada, se cargan sus secretos con `DefaultAzureCredential` |
| `Cors__AllowedOrigins__0` | Origen del frontend permitido por CORS (se pueden añadir `__1`, `__2`...) |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | Activa la telemetría. Si no existe, la API funciona sin ella |

## Endpoints

| Método | Ruta | Descripción |
| :- | :- | :- |
| GET | `/api/tickets?estado=&canal=&prioridad=&page=&pageSize=` | Lista paginada con `items`, `total`, `page`, `pageSize` |
| GET | `/api/tickets/{id}` | Detalle. 404 en ProblemDetails si no existe |
| POST | `/api/tickets` | Crea un ticket. 201 con `Location` |
| PUT | `/api/tickets/{id}` | Reemplaza los campos editables. 200 o 404 |
| PATCH | `/api/tickets/{id}/estado` | Cambio rápido de estado. 200 o 404 |
| DELETE | `/api/tickets/{id}` | 204 o 404 |
| POST | `/api/webhooks/mensajes` | Webhook de WhatsApp |
| GET | `/health` | Estado del servicio |

Los errores de validación devuelven 400 en formato ProblemDetails (RFC 7807) con el detalle por campo. Las excepciones no controladas las captura `GlobalExceptionHandler`: las registra en logs y responde 500 sin stack trace.

### Webhook

`POST /api/webhooks/mensajes` crea un ticket con canal `whatsapp`.

- Un `mensajeId` nuevo responde **201** con `"estado": "creado"`.
- Un `mensajeId` ya procesado responde **200** con `"estado": "ya_procesado"` y el `ticketId` original.
- El servicio consulta primero por `MensajeExternoId`. Si dos entregas llegan a la vez, el índice único de la base rechaza la segunda, el servicio captura el error de SQL Server (2601 o 2627) y responde como duplicado.
- El título del ticket es `WhatsApp de {nombre o teléfono}: {texto}`, truncado a 120 caracteres.

## Decisiones técnicas

- `AppDbContext` solo gestiona la conexión; el mapeo está en `Configurations/TicketConfiguration.cs`. No hay migraciones: el esquema lo crean los scripts de `db/`.
- `Id`, `CreadoEn` y `ActualizadoEn` de una inserción los genera SQL Server. En las actualizaciones, `ActualizadoEn` lo asigna la API con `DateTime.UtcNow`.
- Las respuestas usan DTOs; la entidad nunca se expone. La paginación y los filtros se pasan como records (`PageRequest`, `TicketFilter`).
- `Canal`, `Estado` y `Prioridad` son strings, igual que en el esquema. Los valores permitidos están en `TicketValues.cs` y deben coincidir con los CHECK de SQL.
- `EnableRetryOnFailure` en EF Core, por si Azure SQL serverless está pausada.
- OpenAPI y Swagger UI se publican en todos los entornos para que sean accesibles desde Azure.

## Pruebas

```bash
dotnet test TicketsAPI.sln
```

Hay 28 pruebas en dos grupos:

- **Unitarias** (`Unit/`): `TicketService` con el repositorio simulado con NSubstitute.
- **De integración** (`Integration/`): la API completa con `WebApplicationFactory` contra SQL Server real. Usan la base `TicketsFrefity_Tests`, que se crea sola y se prepara con los scripts de `db/`.

Cubren creación válida e inválida, 404, filtros y paginación, PUT, PATCH, DELETE, webhook nuevo, duplicado y entregas simultáneas, el índice único, `/health`, CORS, OpenAPI, Swagger UI, y que una excepción no controlada devuelva 500 sin detalles internos.

Para usar otra instancia de SQL Server: `TICKETS_TEST_CONNECTION_STRING="Server=...;Database=...;..."`.

## Despliegue en Azure

Todo en el grupo de recursos `TicketsFrefity`.

| Qué | Dónde |
| :- | :- |
| Frontend | https://red-plant-07021450f.6.azurestaticapps.net/ |
| API | https://api-tickets-frefity-e3ffgpanchfebabt.chilecentral-01.azurewebsites.net |
| Swagger / salud | `/swagger` y `/health` de la API |

- **App Service** `api-tickets-frefity` (Linux, F1, .NET 10). Se despliega con GitHub Actions al hacer push a `master`; el workflow publica solo `TicketsAPI/TicketsAPI.csproj`.
- **Azure SQL** `tickets-frefity` / `TicketsFrefity`. Firewall: solo la IP del cliente y "Permitir servicios de Azure"; no hay rangos abiertos.
- **Key Vault** `vault-tickets-frefity`, con el secreto `ConnectionStrings--DefaultConnection`. La identidad administrada de la App Service tiene el rol *Key Vault Secrets User*. En la App Service solo hay `KeyVault__Uri` y `Cors__AllowedOrigins__0`; no hay secretos en texto plano.
- **Static Web Apps** `swa-tickets-frefity`, compilado con `VITE_API_URL` apuntando a la API. CORS en la API solo acepta ese origen.
- **Application Insights** `api-tickets-frefity` (workspace-based). El `Program.cs` activa OpenTelemetry cuando existe `APPLICATIONINSIGHTS_CONNECTION_STRING`. Los logs estructurados de ticket creado, webhook duplicado y excepciones salen por `ILogger`.

Consulta KQL (requests fallidos y excepciones de las últimas 24 horas), en Application Insights → Registros:

```kql
requests
| where timestamp > ago(24h)
| where success == false
```

Excepciones no controladas:

```kql
exceptions
| where timestamp > ago(24h)
```

## Uso de IA

Usé Claude (Claude Code) como asistente.

- **Qué pedí:** estructura del backend por capas, repositorio y servicio, webhook idempotente, pruebas, frontend en React, documentación y ayuda para diagnosticar el despliegue en Azure.
- **Qué corregí:** la cadena de conexión en Key Vault tenía las llaves `{ }` de la plantilla del portal y daba error de login a SQL; la App Service mostraba la página por defecto hasta fijar el comando de inicio `dotnet TicketsAPI.dll`; a la identidad administrada le faltaba el rol *Key Vault Secrets User*.
- **Qué verifiqué yo:** los scripts en Azure SQL (Query editor), el webhook en Postman (201 y luego 200 `ya_procesado`), CORS y el frontend contra la API en Azure, y los logs en Application Insights.

## Pendiente y mejoras

- Autenticación: la API no tiene login; cualquiera con la URL puede modificar tickets.
- Validar la firma del webhook, que un proveedor real incluiría.
- Procesar el webhook con una cola si crece el volumen.
- `/health` solo confirma que la API está viva; podría comprobar la conexión a SQL.
- Que `ActualizadoEn` lo fije la base en las actualizaciones (trigger).
