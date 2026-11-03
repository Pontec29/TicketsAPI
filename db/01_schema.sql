-- Ejecutar conectado a la base destino (TicketsFrefity en local o la base de Azure SQL).
-- La base no se crea aquí: en Azure SQL no se puede. Local: CREATE DATABASE TicketsFrefity;
-- Crear tabla si no existe
IF OBJECT_ID('dbo.Tickets', 'U') IS NULL
BEGIN
    CREATE TABLE Tickets (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        Titulo NVARCHAR(120) NOT NULL,
        Descripcion NVARCHAR(1000) NULL,
        Canal VARCHAR(20) NOT NULL CHECK (Canal IN ('web', 'whatsapp', 'email')),
        Estado VARCHAR(20) NOT NULL DEFAULT 'abierto' CHECK (Estado IN ('abierto', 'en_proceso', 'cerrado')),
        Prioridad VARCHAR(10) NOT NULL DEFAULT 'media' CHECK (Prioridad IN ('baja', 'media', 'alta')),
        MensajeExternoId VARCHAR(100) NULL,
        CreadoEn DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        ActualizadoEn DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
END
GO

-- (a) Crear índice único filtrado para MensajeExternoId
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'UQ_Tickets_MensajeExternoId' AND object_id = OBJECT_ID('Tickets'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UQ_Tickets_MensajeExternoId 
    ON Tickets(MensajeExternoId) 
    WHERE MensajeExternoId IS NOT NULL;
END
GO

-- (b) Crear índice compuesto para Estado y CreadoEn
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Tickets_Estado_CreadoEn' AND object_id = OBJECT_ID('Tickets'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Tickets_Estado_CreadoEn 
    ON Tickets(Estado, CreadoEn);
END
GO