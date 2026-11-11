UPDATE dbo.Orders
SET
    Version = @Version,
    State = @State,
    QuoteAmount = @QuoteAmount,
    ActualKm = @ActualKm,
    ActualDate = @ActualDate
WHERE Id = @Id
  AND Version = @ExpectedVersion;
