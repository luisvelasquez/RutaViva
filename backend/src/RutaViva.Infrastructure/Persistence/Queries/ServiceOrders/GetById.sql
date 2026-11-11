SELECT
    Id,
    Version,
    VehicleId,
    RoutineId,
    CycleId,
    Workshop,
    State,
    QuoteAmount,
    ActualKm,
    ActualDate,
    CreatedAt
FROM dbo.Orders
WHERE Id = @id;
