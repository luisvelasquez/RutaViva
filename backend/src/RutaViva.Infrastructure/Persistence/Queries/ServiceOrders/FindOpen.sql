SELECT COUNT(1)
FROM dbo.Orders
WHERE VehicleId = @vehicleId
  AND RoutineId = @routineId
  AND State NOT IN ('Completed', 'Cancelled');
