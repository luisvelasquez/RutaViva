SELECT
    VehicleId,
    Version,
    LicensePlate,
    AssignedWorkshop,
    OdometerKilometers
FROM dbo.Vehicles
ORDER BY LicensePlate;
