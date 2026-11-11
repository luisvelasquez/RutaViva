SELECT VehicleId
FROM dbo.Vehicles
ORDER BY VehicleId
OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY;
