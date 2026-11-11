-- Datos opcionales de demostración. Ejecutar solo en bases de desarrollo.
-- Requiere esquema 01_schema.sql aplicado.
SET NOCOUNT ON;

IF EXISTS (SELECT 1 FROM dbo.Vehicles WHERE LicensePlate = N'ABC123')
BEGIN
    PRINT 'Seed demo ya presente (ABC123).';
    RETURN;
END;

DECLARE @VehicleId uniqueidentifier = NEWID();
DECLARE @Today date = CAST(SWITCHOFFSET(SYSDATETIMEOFFSET(), '-05:00') AS date);
DECLARE @OilId uniqueidentifier = NEWID();
DECLARE @BrakesId uniqueidentifier = NEWID();
DECLARE @TyresId uniqueidentifier = NEWID();

INSERT INTO dbo.Vehicles (VehicleId, Version, LicensePlate, AssignedWorkshop, OdometerKilometers)
VALUES (@VehicleId, 1, N'ABC123', N'Taller Central', 25000.00);

INSERT INTO dbo.MaintenanceRoutines (
    MaintenanceRoutineId, Version, VehicleId, Name, IntervalKilometers, IntervalDays,
    LastServiceOdometerKilometers, LastServiceDate, MaintenanceCycleId, DueSoonNoticeRaised, IsCycleCancelled, Status)
VALUES
    (@OilId, 1, @VehicleId, N'Cambio de aceite', 10000.00, 180, 16000.00, DATEADD(day, -100, @Today), NEWID(), 0, 0, N'Current'),
    (@BrakesId, 1, @VehicleId, N'Revisión de frenos', 20000.00, NULL, 5000.00, DATEADD(day, -200, @Today), NEWID(), 0, 0, N'Current'),
    (@TyresId, 1, @VehicleId, N'Rotación de llantas', NULL, 90, 23000.00, DATEADD(day, -20, @Today), NEWID(), 0, 0, N'Current');

PRINT 'Seed demo insertado. Ejecute POST /api/maintenance/evaluate para alinear estados y órdenes.';
GO
