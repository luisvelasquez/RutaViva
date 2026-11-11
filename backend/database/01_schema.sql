-- Ejecutar conectado a la base RutaViva.
-- Esquema con nombres en ingles (dominio claro). Para bases existentes con nombres cortos
-- (Vehicles.Id / Routines / Orders / Receipts / Outbox) usar 03_migrate_existing_to_english.sql.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ARITHABORT ON;
SET NUMERIC_ROUNDABORT OFF;
GO

IF OBJECT_ID('dbo.Vehicles','U') IS NULL
BEGIN
    CREATE TABLE dbo.Vehicles (
        VehicleId uniqueidentifier NOT NULL CONSTRAINT PK_Vehicles PRIMARY KEY,
        Version bigint NOT NULL,
        LicensePlate nvarchar(20) NOT NULL,
        AssignedWorkshop nvarchar(120) NOT NULL,
        OdometerKilometers decimal(18,2) NOT NULL CONSTRAINT CK_Vehicles_OdometerKilometers CHECK (OdometerKilometers >= 0),
        CONSTRAINT UX_Vehicles_LicensePlate UNIQUE (LicensePlate)
    );
END;
GO

IF OBJECT_ID('dbo.MaintenanceRoutines','U') IS NULL
BEGIN
    CREATE TABLE dbo.MaintenanceRoutines (
        MaintenanceRoutineId uniqueidentifier NOT NULL CONSTRAINT PK_MaintenanceRoutines PRIMARY KEY,
        Version bigint NOT NULL,
        VehicleId uniqueidentifier NOT NULL CONSTRAINT FK_MaintenanceRoutines_Vehicles REFERENCES dbo.Vehicles(VehicleId),
        Name nvarchar(100) NOT NULL,
        IntervalKilometers decimal(18,2) NULL,
        IntervalDays int NULL, -- dias calendario
        LastServiceOdometerKilometers decimal(18,2) NOT NULL,
        LastServiceDate date NOT NULL,
        MaintenanceCycleId uniqueidentifier NOT NULL,
        DueSoonNoticeRaised bit NOT NULL,
        IsCycleCancelled bit NOT NULL,
        Status nvarchar(20) NOT NULL,
        CONSTRAINT CK_MaintenanceRoutines_Interval CHECK ((IntervalKilometers IS NOT NULL OR IntervalDays IS NOT NULL) AND (IntervalKilometers IS NULL OR IntervalKilometers > 0) AND (IntervalDays IS NULL OR IntervalDays > 0)),
        CONSTRAINT CK_MaintenanceRoutines_LastServiceOdometerKilometers CHECK (LastServiceOdometerKilometers >= 0),
        CONSTRAINT CK_MaintenanceRoutines_Status CHECK (Status IN ('Current','DueSoon','Overdue'))
    );
    CREATE INDEX IX_MaintenanceRoutines_VehicleId ON dbo.MaintenanceRoutines(VehicleId);
END;
GO

IF OBJECT_ID('dbo.ServiceOrders','U') IS NULL
BEGIN
    CREATE TABLE dbo.ServiceOrders (
        ServiceOrderId uniqueidentifier NOT NULL CONSTRAINT PK_ServiceOrders PRIMARY KEY,
        Version bigint NOT NULL,
        VehicleId uniqueidentifier NOT NULL CONSTRAINT FK_ServiceOrders_Vehicles REFERENCES dbo.Vehicles(VehicleId),
        MaintenanceRoutineId uniqueidentifier NOT NULL CONSTRAINT FK_ServiceOrders_MaintenanceRoutines REFERENCES dbo.MaintenanceRoutines(MaintenanceRoutineId),
        MaintenanceCycleId uniqueidentifier NOT NULL,
        AssignedWorkshop nvarchar(120) NOT NULL,
        Status nvarchar(20) NOT NULL,
        QuoteAmountCop decimal(18,2) NULL,
        ActualServiceOdometerKilometers decimal(18,2) NULL,
        ActualServiceDate date NULL,
        CreatedAt datetimeoffset NOT NULL,
        CONSTRAINT CK_ServiceOrders_Status CHECK (Status IN ('Created','Quoted','Approved','InProgress','Completed','Cancelled')),
        CONSTRAINT CK_ServiceOrders_QuoteAmountCop CHECK (QuoteAmountCop IS NULL OR QuoteAmountCop > 0)
    );
    CREATE UNIQUE INDEX UX_ServiceOrders_OpenRoutine ON dbo.ServiceOrders(VehicleId,MaintenanceRoutineId)
        WHERE Status <> 'Completed' AND Status <> 'Cancelled';
END;
GO

IF OBJECT_ID('dbo.IdempotencyRecords','U') IS NULL
BEGIN
    CREATE TABLE dbo.IdempotencyRecords (
        Scope nvarchar(160) NOT NULL,
        IdempotencyKey nvarchar(100) NOT NULL,
        RequestFingerprint varchar(64) NOT NULL,
        StoredResponse nvarchar(max) NOT NULL,
        CreatedAt datetimeoffset NOT NULL,
        CONSTRAINT PK_IdempotencyRecords PRIMARY KEY (Scope,IdempotencyKey)
    );
END;
GO

IF OBJECT_ID('dbo.OutboxMessages','U') IS NULL
BEGIN
    CREATE TABLE dbo.OutboxMessages (
        OutboxMessageId uniqueidentifier NOT NULL CONSTRAINT PK_OutboxMessages PRIMARY KEY,
        EventType nvarchar(80) NOT NULL,
        AggregateId uniqueidentifier NOT NULL,
        MaintenanceCycleId uniqueidentifier NOT NULL,
        Payload nvarchar(500) NOT NULL,
        OccurredAt datetimeoffset NOT NULL,
        PublishedAt datetimeoffset NULL
    );
    CREATE UNIQUE INDEX UX_OutboxMessages_DueSoonNotice ON dbo.OutboxMessages(EventType,AggregateId,MaintenanceCycleId)
        WHERE EventType = 'MaintenanceDueSoon';
    CREATE INDEX IX_OutboxMessages_Pending ON dbo.OutboxMessages(OccurredAt) WHERE PublishedAt IS NULL;
END;
GO
