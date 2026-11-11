using RutaViva.Domain;
using RutaViva.Domain.Aggregates.MaintenanceRoutines;
using RutaViva.Domain.Aggregates.ServiceOrders;
using RutaViva.Domain.Aggregates.Vehicles;
using RutaViva.Domain.Events;
using RutaViva.Domain.ValueObjects;
using Xunit;

namespace RutaViva.Tests;

public sealed class DomainTests
{
    private static readonly DateOnly Today = new(2026, 11, 10);

    private static MaintenanceRoutine Routine(decimal? km = 10000, int? days = 180) =>
        new(Guid.NewGuid(), "Aceite", new(km, days), new(20000), Today);

    private static ServiceOrder Order() =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Taller", DateTimeOffset.UtcNow);

    [Theory]
    [InlineData(28999, MaintenanceStatus.Current)]
    [InlineData(29000, MaintenanceStatus.DueSoon)]
    [InlineData(29999, MaintenanceStatus.DueSoon)]
    [InlineData(30000, MaintenanceStatus.Overdue)]
    [InlineData(35000, MaintenanceStatus.Overdue)]
    public void Kilometre_boundaries(decimal odometer, MaintenanceStatus expected)
    {
        var r = Routine();
        r.EvaluateMaintenanceDueDates(new Kilometres(odometer), Today);
        Assert.Equal(expected, r.Status);
    }

    [Theory]
    [InlineData(161, MaintenanceStatus.Current)]
    [InlineData(162, MaintenanceStatus.DueSoon)]
    [InlineData(179, MaintenanceStatus.DueSoon)]
    [InlineData(180, MaintenanceStatus.Overdue)]
    public void Time_boundaries(int elapsed, MaintenanceStatus expected)
    {
        var r = Routine();
        r.EvaluateMaintenanceDueDates(new Kilometres(20000), Today.AddDays(elapsed));
        Assert.Equal(expected, r.Status);
    }

    [Fact]
    public void Whichever_interval_occurs_first_wins()
    {
        var r = Routine();
        r.EvaluateMaintenanceDueDates(new Kilometres(20001), Today.AddDays(180));
        Assert.Equal(MaintenanceStatus.Overdue, r.Status);

        var other = Routine();
        other.EvaluateMaintenanceDueDates(new Kilometres(30000), Today.AddDays(1));
        Assert.Equal(MaintenanceStatus.Overdue, other.Status);
    }

    [Fact]
    public void Warning_only_once_per_cycle_and_enabled_after_reset()
    {
        var r = Routine();
        r.EvaluateMaintenanceDueDates(new Kilometres(29000), Today);
        Assert.Single(r.DomainEvents);
        Assert.Equal(DomainEventTypes.MaintenanceDueSoon, r.DomainEvents[0].Type);
        r.DequeueDomainEvents();

        r.EvaluateMaintenanceDueDates(new Kilometres(29500), Today);
        Assert.Empty(r.DomainEvents);

        var cycle = r.CycleId;
        r.ResetMaintenanceCycle(new Kilometres(30000), Today, Today);
        Assert.NotEqual(cycle, r.CycleId);
        Assert.False(r.NoticeRaised);
        Assert.Equal(MaintenanceStatus.Current, r.Status);

        r.EvaluateMaintenanceDueDates(new Kilometres(39000), Today);
        Assert.Single(r.DomainEvents);
    }

    [Fact]
    public void Jump_directly_to_overdue_does_not_send_late_warning()
    {
        var r = Routine();
        r.EvaluateMaintenanceDueDates(new Kilometres(30000), Today);
        Assert.Empty(r.DomainEvents);
        Assert.Equal(MaintenanceStatus.Overdue, r.Status);
    }

    [Theory]
    [InlineData(25000)]
    [InlineData(24000)]
    public void Equal_or_old_readings_ignored(decimal reading)
    {
        var v = new Vehicle("abc123", "Taller", new Kilometres(25000));
        Assert.False(v.RecordOdometerReading(new Kilometres(reading)));
        Assert.Equal(25000, v.Odometer.Value);
        Assert.Equal(1, v.Version);
    }

    [Fact]
    public void Greater_reading_accepted()
    {
        var v = new Vehicle("abc123", "Taller", new Kilometres(25000));
        Assert.True(v.RecordOdometerReading(new Kilometres(25001)));
        Assert.Equal(25001, v.Odometer.Value);
    }

    [Theory]
    [InlineData(1, OrderState.Approved)]
    [InlineData(1500000, OrderState.Approved)]
    [InlineData(1500000.01, OrderState.Quoted)]
    public void Approval_threshold(decimal amount, OrderState expected)
    {
        var o = Order();
        o.SubmitServiceQuote(new MoneyCop(amount));
        Assert.Equal(expected, o.State);
    }

    [Fact]
    public void Normal_manual_approval_flow()
    {
        var o = Order();
        o.SubmitServiceQuote(new MoneyCop(2000000));
        o.ApproveServiceQuote();
        o.StartService();
        o.CompleteServiceOrder(new Kilometres(50000), Today);
        Assert.Equal(OrderState.Completed, o.State);
        Assert.Contains(o.DomainEvents, e => e.Type == DomainEventTypes.ServiceCompleted);
        Assert.False(o.IsOpen);
    }

    [Fact]
    public void Rejection_cancels_order()
    {
        var o = Order();
        o.SubmitServiceQuote(new MoneyCop(2000000));
        o.RejectServiceQuote();
        Assert.Equal(OrderState.Cancelled, o.State);
    }

    [Fact]
    public void Cancel_allowed_in_every_pre_execution_state()
    {
        var created = Order();
        created.Cancel();
        Assert.False(created.IsOpen);

        var quoted = Order();
        quoted.SubmitServiceQuote(new MoneyCop(2000000));
        quoted.Cancel();
        Assert.False(quoted.IsOpen);

        var approved = Order();
        approved.SubmitServiceQuote(new MoneyCop(100));
        approved.Cancel();
        Assert.False(approved.IsOpen);
    }

    [Fact]
    public void Cannot_cancel_after_start_or_complete_without_start()
    {
        var o = Order();
        Assert.Throws<BusinessException>(() => o.CompleteServiceOrder(new Kilometres(100), Today));
        o.SubmitServiceQuote(new MoneyCop(100));
        o.StartService();
        Assert.Throws<BusinessException>(o.Cancel);
    }

    [Fact]
    public void Cannot_skip_states_or_quote_again()
    {
        var o = Order();
        Assert.Throws<BusinessException>(o.StartService);
        Assert.Throws<BusinessException>(o.ApproveServiceQuote);
        o.SubmitServiceQuote(new MoneyCop(100));
        Assert.Throws<BusinessException>(() => o.SubmitServiceQuote(new MoneyCop(200)));
    }

    [Fact]
    public void Stale_version_rejected()
    {
        var o = Order();
        o.SubmitServiceQuote(new MoneyCop(100));
        Assert.Throws<ConflictException>(() => o.RequireVersion(1));
        o.RequireVersion(2);
    }

    [Fact]
    public void Invalid_value_objects_rejected()
    {
        Assert.Throws<BusinessException>(() => new Kilometres(-1));
        Assert.Throws<BusinessException>(() => new MoneyCop(0));
        Assert.Throws<BusinessException>(() => new MaintenanceInterval(null, null));
        Assert.Throws<BusinessException>(() => new MaintenanceInterval(0, 10));
    }

    [Fact]
    public void Value_objects_compare_by_value()
    {
        Assert.Equal(new Kilometres(10), new Kilometres(10));
        Assert.Equal(new MoneyCop(100), new MoneyCop(100));
        Assert.Equal(new MaintenanceInterval(1000, 30), new MaintenanceInterval(1000, 30));
    }

    [Fact]
    public void Service_cannot_precede_previous_service_or_be_in_future()
    {
        var r = Routine();
        Assert.Throws<BusinessException>(() => r.ResetMaintenanceCycle(new Kilometres(19999), Today, Today));
        Assert.Throws<BusinessException>(() => r.ResetMaintenanceCycle(new Kilometres(20000), Today.AddDays(-1), Today));
        Assert.Throws<BusinessException>(() => r.ResetMaintenanceCycle(new Kilometres(20000), Today.AddDays(1), Today));
    }

    [Fact]
    public void Cancellation_suppresses_further_cycle_processing()
    {
        var r = Routine();
        r.SuppressCancelledCycle();
        r.EvaluateMaintenanceDueDates(new Kilometres(30000), Today.AddDays(200));
        Assert.Empty(r.DomainEvents);
        Assert.True(r.CycleCancelled);
    }

    [Fact]
    public void Single_dimension_routines_supported()
    {
        var km = Routine(10000, null);
        km.EvaluateMaintenanceDueDates(new Kilometres(20000), Today.AddDays(1000));
        Assert.Equal(MaintenanceStatus.Current, km.Status);

        var time = Routine(null, 180);
        time.EvaluateMaintenanceDueDates(new Kilometres(100000), Today);
        Assert.Equal(MaintenanceStatus.Current, time.Status);
    }

    [Fact]
    public void Reconstitute_does_not_raise_creation_events()
    {
        var vehicle = Vehicle.Reconstitute(Guid.NewGuid(), 3, "ABC123", "Taller", 1000);
        Assert.Empty(vehicle.DomainEvents);
        Assert.Equal(3, vehicle.Version);
    }
}
