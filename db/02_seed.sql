-- Datos de prueba. Re-ejecutable: MERGE inserta solo los Id que aún no existen.
-- Ejecutar después de 01_schema.sql.
MERGE dbo.Tickets AS destino
USING (VALUES
    ('10000000-0000-0000-0000-000000000001', 'Problema de acceso', 'No reconoce mi contraseña', 'web', 'abierto', 'alta', NULL),
    ('10000000-0000-0000-0000-000000000002', 'Consulta de saldo', NULL, 'whatsapp', 'cerrado', 'baja', 'msg_wa_001'),
    ('10000000-0000-0000-0000-000000000003', 'Cobro duplicado', 'Me cobraron dos veces', 'email', 'en_proceso', 'alta', 'msg_em_001'),
    ('10000000-0000-0000-0000-000000000004', 'Duda sobre envíos', '¿Llegan a provincia?', 'web', 'abierto', 'media', NULL),
    ('10000000-0000-0000-0000-000000000005', 'Solicito reembolso', NULL, 'whatsapp', 'abierto', 'alta', 'msg_wa_002'),
    ('10000000-0000-0000-0000-000000000006', 'Actualizar mis datos', 'Cambio de domicilio', 'email', 'cerrado', 'media', 'msg_em_002'),
    ('10000000-0000-0000-0000-000000000007', 'La app se cierra sola', 'Crash en Android', 'web', 'en_proceso', 'media', NULL),
    ('10000000-0000-0000-0000-000000000008', 'Horarios de atención', NULL, 'whatsapp', 'cerrado', 'baja', 'msg_wa_003'),
    ('10000000-0000-0000-0000-000000000009', 'Sugerencia de mejora', 'Modo oscuro por favor', 'email', 'abierto', 'baja', 'msg_em_003'),
    ('10000000-0000-0000-0000-000000000010', 'No carga mi perfil', 'Se queda en blanco', 'web', 'en_proceso', 'media', NULL)
) AS origen (Id, Titulo, Descripcion, Canal, Estado, Prioridad, MensajeExternoId)
ON destino.Id = CAST(origen.Id AS UNIQUEIDENTIFIER)
WHEN NOT MATCHED THEN
    INSERT (Id, Titulo, Descripcion, Canal, Estado, Prioridad, MensajeExternoId)
    VALUES (CAST(origen.Id AS UNIQUEIDENTIFIER), origen.Titulo, origen.Descripcion, origen.Canal, origen.Estado, origen.Prioridad, origen.MensajeExternoId);
GO
