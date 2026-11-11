INSERT INTO dbo.Orders (
    Id,
    Version,
    VehicleId,
    RoutineId,
    CycleId,
    Workshop,
    State,
    QuoteAmount,
    ActualKm,
    ActualDate,
    CreatedAt)
VALUES (
    @Id,
    @Version,
    @VehicleId,
    @RoutineId,
    @CycleId,
    @Workshop,
    @State,
    @QuoteAmount,
    @ActualKm,
    @ActualDate,
    @CreatedAt);
