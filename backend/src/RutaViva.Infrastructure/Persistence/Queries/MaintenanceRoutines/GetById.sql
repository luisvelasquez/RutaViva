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
WHERE Id = @id;
