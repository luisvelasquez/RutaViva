UPDATE dbo.Vehicles
SET
    Version = @Version,
    Plate = @Plate,
    Workshop = @Workshop,
    Odometer = @Odometer
WHERE Id = @Id
  AND Version = @ExpectedVersion;
