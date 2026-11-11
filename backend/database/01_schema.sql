-- Ejecutar conectado a la base RutaViva.
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
        Id uniqueidentifier NOT NULL CONSTRAINT PK_Vehicles PRIMARY KEY,
        Version bigint NOT NULL,
        Plate nvarchar(20) NOT NULL,
        Workshop nvarchar(120) NOT NULL,
        Odometer decimal(18,2) NOT NULL CONSTRAINT CK_Vehicles_Odometer CHECK (Odometer >= 0),
        CONSTRAINT UX_Vehicles_Plate UNIQUE (Plate)
    );
END;
GO

IF OBJECT_ID('dbo.Routines','U') IS NULL
BEGIN
    CREATE TABLE dbo.Routines (
        Id uniqueidentifier NOT NULL CONSTRAINT PK_Routines PRIMARY KEY,
        Version bigint NOT NULL,
        VehicleId uniqueidentifier NOT NULL CONSTRAINT FK_Routines_Vehicles REFERENCES dbo.Vehicles(Id),
        Name nvarchar(100) NOT NULL,
        IntervalKm decimal(18,2) NULL,
        IntervalDays int NULL,
        LastServiceKm decimal(18,2) NOT NULL,
        LastServiceDate date NOT NULL,
        CycleId uniqueidentifier NOT NULL,
        NoticeRaised bit NOT NULL,
        CycleCancelled bit NOT NULL,
        Status nvarchar(20) NOT NULL,
        CONSTRAINT CK_Routines_Interval CHECK ((IntervalKm IS NOT NULL OR IntervalDays IS NOT NULL) AND (IntervalKm IS NULL OR IntervalKm > 0) AND (IntervalDays IS NULL OR IntervalDays > 0)),
        CONSTRAINT CK_Routines_Km CHECK (LastServiceKm >= 0),
        CONSTRAINT CK_Routines_Status CHECK (Status IN ('Current','DueSoon','Overdue'))
    );
    CREATE INDEX IX_Routines_VehicleId ON dbo.Routines(VehicleId);
END;
GO

IF OBJECT_ID('dbo.Orders','U') IS NULL
BEGIN
    CREATE TABLE dbo.Orders (
        Id uniqueidentifier NOT NULL CONSTRAINT PK_Orders PRIMARY KEY,
        Version bigint NOT NULL,
        VehicleId uniqueidentifier NOT NULL CONSTRAINT FK_Orders_Vehicles REFERENCES dbo.Vehicles(Id),
        RoutineId uniqueidentifier NOT NULL CONSTRAINT FK_Orders_Routines REFERENCES dbo.Routines(Id),
        CycleId uniqueidentifier NOT NULL,
        Workshop nvarchar(120) NOT NULL,
        State nvarchar(20) NOT NULL,
        QuoteAmount decimal(18,2) NULL,
        ActualKm decimal(18,2) NULL,
        ActualDate date NULL,
        CreatedAt datetimeoffset NOT NULL,
        CONSTRAINT CK_Orders_State CHECK (State IN ('Created','Quoted','Approved','InProgress','Completed','Cancelled')),
        CONSTRAINT CK_Orders_Amount CHECK (QuoteAmount IS NULL OR QuoteAmount > 0)
    );
    CREATE UNIQUE INDEX UX_Orders_OpenRoutine ON dbo.Orders(VehicleId,RoutineId)
        WHERE State <> 'Completed' AND State <> 'Cancelled';
END;
GO

IF OBJECT_ID('dbo.Receipts','U') IS NULL
BEGIN
    CREATE TABLE dbo.Receipts (
        Scope nvarchar(160) NOT NULL,
        [Key] nvarchar(100) NOT NULL,
        Fingerprint varchar(64) NOT NULL,
        Response nvarchar(max) NOT NULL,
        CreatedAt datetimeoffset NOT NULL,
        CONSTRAINT PK_Receipts PRIMARY KEY (Scope,[Key])
    );
END;
GO

IF OBJECT_ID('dbo.Outbox','U') IS NULL
BEGIN
    CREATE TABLE dbo.Outbox (
        Id uniqueidentifier NOT NULL CONSTRAINT PK_Outbox PRIMARY KEY,
        Type nvarchar(80) NOT NULL,
        AggregateId uniqueidentifier NOT NULL,
        CycleId uniqueidentifier NOT NULL,
        Message nvarchar(500) NOT NULL,
        OccurredAt datetimeoffset NOT NULL,
        PublishedAt datetimeoffset NULL
    );
    CREATE UNIQUE INDEX UX_Outbox_NoticeCycle ON dbo.Outbox(Type,AggregateId,CycleId)
        WHERE Type = 'MaintenanceDueSoon';
    CREATE INDEX IX_Outbox_Pending ON dbo.Outbox(OccurredAt) WHERE PublishedAt IS NULL;
END;
GO
