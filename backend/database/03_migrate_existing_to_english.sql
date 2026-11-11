-- =============================================================================
-- Migration: short English schema -> clearer English schema (see 01_schema.sql)
-- Fresh installs: 00 + 01 (+ optional 02). Existing short schema: this script.
-- Uses dynamic SQL after renames so the batch compiles against the old metadata.
-- Stop the API. Connect to RutaViva.
-- =============================================================================
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ARITHABORT ON;
GO

IF OBJECT_ID('dbo.Vehiculos','U') IS NOT NULL
BEGIN
    PRINT 'Spanish schema detected (dbo.Vehiculos). Migration skipped.';
END
ELSE IF OBJECT_ID('dbo.MaintenanceRoutines','U') IS NOT NULL
BEGIN
    PRINT 'dbo.MaintenanceRoutines already exists. Migration skipped.';
END
ELSE IF OBJECT_ID('dbo.Routines','U') IS NULL OR OBJECT_ID('dbo.Vehicles','U') IS NULL
BEGIN
    PRINT 'Previous schema not found. Use 01_schema.sql for a fresh install.';
END
ELSE
BEGIN
BEGIN TRANSACTION;
BEGIN TRY
    -- Drop FKs
    IF OBJECT_ID(N'dbo.FK_Orders_Routines','F') IS NOT NULL ALTER TABLE dbo.Orders DROP CONSTRAINT FK_Orders_Routines;
    IF OBJECT_ID(N'dbo.FK_Orders_Vehicles','F') IS NOT NULL ALTER TABLE dbo.Orders DROP CONSTRAINT FK_Orders_Vehicles;
    IF OBJECT_ID(N'dbo.FK_Routines_Vehicles','F') IS NOT NULL ALTER TABLE dbo.Routines DROP CONSTRAINT FK_Routines_Vehicles;

    -- Vehicles
    IF OBJECT_ID(N'dbo.CK_Vehicles_Odometer','C') IS NOT NULL ALTER TABLE dbo.Vehicles DROP CONSTRAINT CK_Vehicles_Odometer;
    IF OBJECT_ID(N'dbo.UX_Vehicles_Plate','UQ') IS NOT NULL ALTER TABLE dbo.Vehicles DROP CONSTRAINT UX_Vehicles_Plate;
    IF COL_LENGTH('dbo.Vehicles','Id') IS NOT NULL EXEC sp_rename N'dbo.Vehicles.Id', N'VehicleId', N'COLUMN';
    IF COL_LENGTH('dbo.Vehicles','Plate') IS NOT NULL EXEC sp_rename N'dbo.Vehicles.Plate', N'LicensePlate', N'COLUMN';
    IF COL_LENGTH('dbo.Vehicles','Workshop') IS NOT NULL EXEC sp_rename N'dbo.Vehicles.Workshop', N'AssignedWorkshop', N'COLUMN';
    IF COL_LENGTH('dbo.Vehicles','Odometer') IS NOT NULL EXEC sp_rename N'dbo.Vehicles.Odometer', N'OdometerKilometers', N'COLUMN';
    EXEC sys.sp_executesql N'ALTER TABLE dbo.Vehicles WITH CHECK ADD CONSTRAINT CK_Vehicles_OdometerKilometers CHECK (OdometerKilometers >= 0)';
    EXEC sys.sp_executesql N'ALTER TABLE dbo.Vehicles WITH CHECK ADD CONSTRAINT UX_Vehicles_LicensePlate UNIQUE (LicensePlate)';

    -- Routines
    IF OBJECT_ID(N'dbo.CK_Routines_Interval','C') IS NOT NULL ALTER TABLE dbo.Routines DROP CONSTRAINT CK_Routines_Interval;
    IF OBJECT_ID(N'dbo.CK_Routines_Km','C') IS NOT NULL ALTER TABLE dbo.Routines DROP CONSTRAINT CK_Routines_Km;
    IF OBJECT_ID(N'dbo.CK_Routines_Status','C') IS NOT NULL ALTER TABLE dbo.Routines DROP CONSTRAINT CK_Routines_Status;
    IF COL_LENGTH('dbo.Routines','Id') IS NOT NULL EXEC sp_rename N'dbo.Routines.Id', N'MaintenanceRoutineId', N'COLUMN';
    IF COL_LENGTH('dbo.Routines','IntervalKm') IS NOT NULL EXEC sp_rename N'dbo.Routines.IntervalKm', N'IntervalKilometers', N'COLUMN';
    IF COL_LENGTH('dbo.Routines','LastServiceKm') IS NOT NULL EXEC sp_rename N'dbo.Routines.LastServiceKm', N'LastServiceOdometerKilometers', N'COLUMN';
    IF COL_LENGTH('dbo.Routines','CycleId') IS NOT NULL EXEC sp_rename N'dbo.Routines.CycleId', N'MaintenanceCycleId', N'COLUMN';
    IF COL_LENGTH('dbo.Routines','NoticeRaised') IS NOT NULL EXEC sp_rename N'dbo.Routines.NoticeRaised', N'DueSoonNoticeRaised', N'COLUMN';
    IF COL_LENGTH('dbo.Routines','CycleCancelled') IS NOT NULL EXEC sp_rename N'dbo.Routines.CycleCancelled', N'IsCycleCancelled', N'COLUMN';

    -- Orders
    IF OBJECT_ID(N'dbo.CK_Orders_State','C') IS NOT NULL ALTER TABLE dbo.Orders DROP CONSTRAINT CK_Orders_State;
    IF OBJECT_ID(N'dbo.CK_Orders_Amount','C') IS NOT NULL ALTER TABLE dbo.Orders DROP CONSTRAINT CK_Orders_Amount;
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Orders_OpenRoutine' AND object_id = OBJECT_ID(N'dbo.Orders'))
        DROP INDEX UX_Orders_OpenRoutine ON dbo.Orders;
    IF COL_LENGTH('dbo.Orders','Id') IS NOT NULL EXEC sp_rename N'dbo.Orders.Id', N'ServiceOrderId', N'COLUMN';
    IF COL_LENGTH('dbo.Orders','RoutineId') IS NOT NULL EXEC sp_rename N'dbo.Orders.RoutineId', N'MaintenanceRoutineId', N'COLUMN';
    IF COL_LENGTH('dbo.Orders','CycleId') IS NOT NULL EXEC sp_rename N'dbo.Orders.CycleId', N'MaintenanceCycleId', N'COLUMN';
    IF COL_LENGTH('dbo.Orders','Workshop') IS NOT NULL EXEC sp_rename N'dbo.Orders.Workshop', N'AssignedWorkshop', N'COLUMN';
    IF COL_LENGTH('dbo.Orders','State') IS NOT NULL EXEC sp_rename N'dbo.Orders.State', N'Status', N'COLUMN';
    IF COL_LENGTH('dbo.Orders','QuoteAmount') IS NOT NULL EXEC sp_rename N'dbo.Orders.QuoteAmount', N'QuoteAmountCop', N'COLUMN';
    IF COL_LENGTH('dbo.Orders','ActualKm') IS NOT NULL EXEC sp_rename N'dbo.Orders.ActualKm', N'ActualServiceOdometerKilometers', N'COLUMN';
    IF COL_LENGTH('dbo.Orders','ActualDate') IS NOT NULL EXEC sp_rename N'dbo.Orders.ActualDate', N'ActualServiceDate', N'COLUMN';

    -- Receipts
    IF COL_LENGTH('dbo.Receipts','Key') IS NOT NULL EXEC sp_rename N'dbo.Receipts.Key', N'IdempotencyKey', N'COLUMN';
    IF COL_LENGTH('dbo.Receipts','Fingerprint') IS NOT NULL EXEC sp_rename N'dbo.Receipts.Fingerprint', N'RequestFingerprint', N'COLUMN';
    IF COL_LENGTH('dbo.Receipts','Response') IS NOT NULL EXEC sp_rename N'dbo.Receipts.Response', N'StoredResponse', N'COLUMN';

    -- Outbox
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Outbox_NoticeCycle' AND object_id = OBJECT_ID(N'dbo.Outbox'))
        DROP INDEX UX_Outbox_NoticeCycle ON dbo.Outbox;
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Outbox_Pending' AND object_id = OBJECT_ID(N'dbo.Outbox'))
        DROP INDEX IX_Outbox_Pending ON dbo.Outbox;
    IF COL_LENGTH('dbo.Outbox','Id') IS NOT NULL EXEC sp_rename N'dbo.Outbox.Id', N'OutboxMessageId', N'COLUMN';
    IF COL_LENGTH('dbo.Outbox','Type') IS NOT NULL EXEC sp_rename N'dbo.Outbox.Type', N'EventType', N'COLUMN';
    IF COL_LENGTH('dbo.Outbox','CycleId') IS NOT NULL EXEC sp_rename N'dbo.Outbox.CycleId', N'MaintenanceCycleId', N'COLUMN';
    IF COL_LENGTH('dbo.Outbox','Message') IS NOT NULL EXEC sp_rename N'dbo.Outbox.Message', N'Payload', N'COLUMN';

    -- Rename tables
    EXEC sp_rename N'dbo.Routines', N'MaintenanceRoutines';
    EXEC sp_rename N'dbo.Orders', N'ServiceOrders';
    EXEC sp_rename N'dbo.Receipts', N'IdempotencyRecords';
    EXEC sp_rename N'dbo.Outbox', N'OutboxMessages';

    -- Recreate FKs / CHECKs / indexes (dynamic: new names)
    EXEC sys.sp_executesql N'ALTER TABLE dbo.MaintenanceRoutines WITH CHECK ADD CONSTRAINT FK_MaintenanceRoutines_Vehicles FOREIGN KEY (VehicleId) REFERENCES dbo.Vehicles(VehicleId)';
    EXEC sys.sp_executesql N'ALTER TABLE dbo.ServiceOrders WITH CHECK ADD CONSTRAINT FK_ServiceOrders_Vehicles FOREIGN KEY (VehicleId) REFERENCES dbo.Vehicles(VehicleId)';
    EXEC sys.sp_executesql N'ALTER TABLE dbo.ServiceOrders WITH CHECK ADD CONSTRAINT FK_ServiceOrders_MaintenanceRoutines FOREIGN KEY (MaintenanceRoutineId) REFERENCES dbo.MaintenanceRoutines(MaintenanceRoutineId)';

    EXEC sys.sp_executesql N'ALTER TABLE dbo.MaintenanceRoutines WITH CHECK ADD CONSTRAINT CK_MaintenanceRoutines_Interval CHECK ((IntervalKilometers IS NOT NULL OR IntervalDays IS NOT NULL) AND (IntervalKilometers IS NULL OR IntervalKilometers > 0) AND (IntervalDays IS NULL OR IntervalDays > 0))';
    EXEC sys.sp_executesql N'ALTER TABLE dbo.MaintenanceRoutines WITH CHECK ADD CONSTRAINT CK_MaintenanceRoutines_LastServiceOdometerKilometers CHECK (LastServiceOdometerKilometers >= 0)';
    EXEC sys.sp_executesql N'ALTER TABLE dbo.MaintenanceRoutines WITH CHECK ADD CONSTRAINT CK_MaintenanceRoutines_Status CHECK (Status IN (''Current'',''DueSoon'',''Overdue''))';

    EXEC sys.sp_executesql N'ALTER TABLE dbo.ServiceOrders WITH CHECK ADD CONSTRAINT CK_ServiceOrders_Status CHECK (Status IN (''Created'',''Quoted'',''Approved'',''InProgress'',''Completed'',''Cancelled''))';
    EXEC sys.sp_executesql N'ALTER TABLE dbo.ServiceOrders WITH CHECK ADD CONSTRAINT CK_ServiceOrders_QuoteAmountCop CHECK (QuoteAmountCop IS NULL OR QuoteAmountCop > 0)';
    EXEC sys.sp_executesql N'CREATE UNIQUE INDEX UX_ServiceOrders_OpenRoutine ON dbo.ServiceOrders(VehicleId, MaintenanceRoutineId) WHERE Status <> ''Completed'' AND Status <> ''Cancelled''';

    EXEC sys.sp_executesql N'CREATE UNIQUE INDEX UX_OutboxMessages_DueSoonNotice ON dbo.OutboxMessages(EventType, AggregateId, MaintenanceCycleId) WHERE EventType = ''MaintenanceDueSoon''';
    EXEC sys.sp_executesql N'CREATE INDEX IX_OutboxMessages_Pending ON dbo.OutboxMessages(OccurredAt) WHERE PublishedAt IS NULL';

    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Routines_VehicleId' AND object_id = OBJECT_ID(N'dbo.MaintenanceRoutines'))
        EXEC sp_rename N'dbo.MaintenanceRoutines.IX_Routines_VehicleId', N'IX_MaintenanceRoutines_VehicleId', N'INDEX';

    IF OBJECT_ID(N'dbo.PK_Routines','PK') IS NOT NULL EXEC sp_rename N'dbo.PK_Routines', N'PK_MaintenanceRoutines', N'OBJECT';
    IF OBJECT_ID(N'dbo.PK_Orders','PK') IS NOT NULL EXEC sp_rename N'dbo.PK_Orders', N'PK_ServiceOrders', N'OBJECT';
    IF OBJECT_ID(N'dbo.PK_Receipts','PK') IS NOT NULL EXEC sp_rename N'dbo.PK_Receipts', N'PK_IdempotencyRecords', N'OBJECT';
    IF OBJECT_ID(N'dbo.PK_Outbox','PK') IS NOT NULL EXEC sp_rename N'dbo.PK_Outbox', N'PK_OutboxMessages', N'OBJECT';

    COMMIT TRANSACTION;
    PRINT 'Migration to clearer English schema completed.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
END;
GO
