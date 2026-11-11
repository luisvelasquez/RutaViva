UPDATE dbo.MaintenanceRoutines
SET
    Version = @Version,
    Name = @Name,
    IntervalKilometers = @IntervalKm,
    IntervalDays = @IntervalDays,
    LastServiceOdometerKilometers = @LastServiceKm,
    LastServiceDate = @LastServiceDate,
    MaintenanceCycleId = @CycleId,
    DueSoonNoticeRaised = @NoticeRaised,
    IsCycleCancelled = @CycleCancelled,
    Status = @Status
WHERE MaintenanceRoutineId = @Id
  AND Version = @ExpectedVersion;
