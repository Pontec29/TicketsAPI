# TicketsAPI

API REST para gestionar tickets de soporte (web, WhatsApp y email). .NET 10, ASP.NET Core, Entity Framework Core y SQL Server / Azure SQL.

El despliegue en Azure, la base de datos, el frontend y el uso de IA están en el README global de la carpeta padre.

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

Requisitos: .NET SDK 10 y una base SQL Server `TicketsFrefity` creada con los scripts de `db/` (ver README global).

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
