SELECT
    Id,
    Version,
    Plate,
    Workshop,
    Odometer
FROM dbo.Vehicles
WHERE Id = @id;
