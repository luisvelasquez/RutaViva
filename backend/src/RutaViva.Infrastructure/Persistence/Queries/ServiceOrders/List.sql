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
WHERE (@vehicleId IS NULL OR VehicleId = @vehicleId)
  AND (@status IS NULL OR Status = @status)
ORDER BY CreatedAt DESC;
