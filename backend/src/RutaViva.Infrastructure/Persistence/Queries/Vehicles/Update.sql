UPDATE dbo.Vehicles
SET
    Version = @Version,
    LicensePlate = @Plate,
    AssignedWorkshop = @Workshop,
    OdometerKilometers = @Odometer
WHERE VehicleId = @Id
  AND Version = @ExpectedVersion;
