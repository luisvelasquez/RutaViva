UPDATE dbo.ServiceOrders
SET
    Version = @Version,
    AssignedWorkshop = @Workshop,
    Status = @State,
    QuoteAmountCop = @QuoteAmount,
    ActualServiceOdometerKilometers = @ActualKm,
    ActualServiceDate = @ActualDate
WHERE ServiceOrderId = @Id
  AND Version = @ExpectedVersion;
