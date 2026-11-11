UPDATE dbo.Routines
SET
    Version = @Version,
    Name = @Name,
    IntervalKm = @IntervalKm,
    IntervalDays = @IntervalDays,
    LastServiceKm = @LastServiceKm,
    LastServiceDate = @LastServiceDate,
    CycleId = @CycleId,
    NoticeRaised = @NoticeRaised,
    CycleCancelled = @CycleCancelled,
    Status = @Status
WHERE Id = @Id
  AND Version = @ExpectedVersion;
