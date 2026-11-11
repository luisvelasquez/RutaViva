INSERT INTO dbo.ServiceOrders (
    ServiceOrderId,
    Version,
    VehicleId,
    MaintenanceRoutineId,
    MaintenanceCycleId,
    AssignedWorkshop,
    Status,
    QuoteAmountCop,
    ActualServiceOdometerKilometers,
    ActualServiceDate,
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
