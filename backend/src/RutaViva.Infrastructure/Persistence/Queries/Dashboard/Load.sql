SELECT Id, Version, Plate, Workshop, Odometer
FROM dbo.Vehicles
ORDER BY Plate;

SELECT
    Id, Version, VehicleId, Name, IntervalKm, IntervalDays,
    LastServiceKm, LastServiceDate, CycleId, NoticeRaised, CycleCancelled, Status
FROM dbo.Routines
ORDER BY Name;

SELECT
    Id, Version, VehicleId, RoutineId, CycleId, Workshop, State,
    QuoteAmount, ActualKm, ActualDate, CreatedAt
FROM dbo.Orders
ORDER BY CreatedAt DESC;

SELECT TOP (100)
    Id, Type, AggregateId, CycleId, Message, OccurredAt, PublishedAt
FROM dbo.Outbox
ORDER BY OccurredAt DESC;
