SELECT
    Id,
    Version,
    VehicleId,
    Name,
    IntervalKm,
    IntervalDays,
    LastServiceKm,
    LastServiceDate,
    CycleId,
    NoticeRaised,
    CycleCancelled,
    Status
FROM dbo.Routines
WHERE VehicleId = @vehicleId
ORDER BY Name;
