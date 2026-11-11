SELECT
    VehicleId,
    Version,
    LicensePlate,
    AssignedWorkshop,
    OdometerKilometers
FROM dbo.Vehicles
WHERE VehicleId = @id;
