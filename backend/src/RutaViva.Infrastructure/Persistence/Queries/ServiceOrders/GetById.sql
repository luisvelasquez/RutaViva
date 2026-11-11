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
WHERE ServiceOrderId = @id;
