SELECT
    MaintenanceRoutineId,
    Version,
    VehicleId,
    Name,
    IntervalKilometers,
    IntervalDays,
    LastServiceOdometerKilometers,
    LastServiceDate,
    MaintenanceCycleId,
    DueSoonNoticeRaised,
    IsCycleCancelled,
    Status
FROM dbo.MaintenanceRoutines
WHERE MaintenanceRoutineId = @id;
