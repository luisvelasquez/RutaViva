SELECT
    VehicleId,
    Version,
    LicensePlate,
    AssignedWorkshop,
    OdometerKilometers
FROM dbo.Vehicles
ORDER BY LicensePlate;

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
ORDER BY Name;

SELECT
    ServiceOrderId,
    Version,
    VehicleId,
    MaintenanceRoutineId,
    MaintenanceCycleId,
    AssignedWorkshop,
    Status,
    QuoteAmountCop,
    ActualServiceOdometerKilometers,
    ActualServiceDate,
    CreatedAt
FROM dbo.ServiceOrders
ORDER BY CreatedAt DESC;

SELECT TOP (100)
    OutboxMessageId,
    EventType,
    AggregateId,
    MaintenanceCycleId,
    Payload,
    OccurredAt,
    PublishedAt
FROM dbo.OutboxMessages
ORDER BY OccurredAt DESC;
