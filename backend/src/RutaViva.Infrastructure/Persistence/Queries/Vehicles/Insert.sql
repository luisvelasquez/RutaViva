INSERT INTO dbo.Vehicles (
    VehicleId,
    Version,
    LicensePlate,
    AssignedWorkshop,
    OdometerKilometers)
VALUES (
    @Id,
    @Version,
    @Plate,
    @Workshop,
    @Odometer);
