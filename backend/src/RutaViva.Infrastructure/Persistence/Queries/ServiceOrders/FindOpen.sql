SELECT COUNT(1)
FROM dbo.ServiceOrders
WHERE VehicleId = @vehicleId
  AND MaintenanceRoutineId = @routineId
  AND Status NOT IN (N'Completed', N'Cancelled');
