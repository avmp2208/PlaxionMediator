using System.Diagnostics.Metrics;
using PlaxionMediator.Abstractions;
using PlaxionMediator.Authorization;
using Xunit;

namespace PlaxionMediator.Authorization.Tests;

[CollectionDefinition("AuthorizationMetrics", DisableParallelization = true)]
public sealed class AuthorizationMetricsCollectionDefinition;

[Collection("AuthorizationMetrics")]
public class AuthorizationMeterTests : IDisposable
{
    private readonly MeterListener _meterListener;
    private long _allowedCount;
    private long _deniedCount;
    private double _totalDuration;
    private int _durationCount;
    private string? _lastRequestType;

    public AuthorizationMeterTests()
    {
        _meterListener = new MeterListener();
        _meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == AuthorizationMeter.Name)
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        _meterListener.SetMeasurementEventCallback<long>((instrument, measurement, tags, state) =>
        {
            if (instrument.Name == "plaxionmediator.authorization.allowed")
            {
                _allowedCount += measurement;
            }
            else if (instrument.Name == "plaxionmediator.authorization.denied")
            {
                _deniedCount += measurement;
            }
            ExtractTags(tags);
        });
        _meterListener.SetMeasurementEventCallback<double>((instrument, measurement, tags, state) =>
        {
            if (instrument.Name == "plaxionmediator.authorization.duration")
            {
                _totalDuration += measurement;
                _durationCount++;
            }
            ExtractTags(tags);
        });
        _meterListener.Start();
    }

    private void ExtractTags(ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        foreach (var tag in tags)
        {
            if (tag.Key == AuthorizationMeter.RequestTypeTag)
            {
                _lastRequestType = tag.Value?.ToString();
            }
        }
    }

    public void Dispose()
    {
        _meterListener.Dispose();
    }

    [Fact]
    public async Task Handle_Authorized_IncrementsAllowedCountAndRecordsDuration()
    {
        // Arrange
        var behavior = new AuthorizationBehavior<TestRequest, string>(
            new[] { new FakeCheck(AuthorizationOutcome.Authorized) },
            new FakeContextAccessor());
        
        // Act
        await behavior.Handle(new TestRequest(), () => ValueTask.FromResult("ok"), CancellationToken.None);

        // Assert
        _meterListener.RecordObservableInstruments(); // Not strictly needed for Counters/Histograms but good practice
        Assert.Equal(1, _allowedCount);
        Assert.Equal(0, _deniedCount);
        Assert.Equal(1, _durationCount);
        Assert.True(_totalDuration >= 0);
        Assert.Equal(typeof(TestRequest).FullName, _lastRequestType);
    }

    [Fact]
    public async Task Handle_Forbidden_IncrementsDeniedCountAndRecordsDuration()
    {
        // Arrange
        var behavior = new AuthorizationBehavior<TestRequest, string>(
            new[] { new FakeCheck(AuthorizationOutcome.Forbidden) },
            new FakeContextAccessor());
        
        // Act & Assert
        await Assert.ThrowsAsync<PlaxionMediatorForbiddenException>(() => 
            behavior.Handle(new TestRequest(), () => ValueTask.FromResult("ok"), CancellationToken.None).AsTask());

        _meterListener.RecordObservableInstruments();
        Assert.Equal(0, _allowedCount);
        Assert.Equal(1, _deniedCount);
        Assert.Equal(1, _durationCount);
        Assert.Equal(typeof(TestRequest).FullName, _lastRequestType);
    }

    [Fact]
    public async Task Handle_Unauthenticated_IncrementsDeniedCountAndRecordsDuration()
    {
        // Arrange
        var behavior = new AuthorizationBehavior<TestRequest, string>(
            new[] { new FakeCheck(AuthorizationOutcome.Unauthenticated) },
            new FakeContextAccessor());
        
        // Act & Assert
        await Assert.ThrowsAsync<PlaxionMediatorUnauthenticatedException>(() => 
            behavior.Handle(new TestRequest(), () => ValueTask.FromResult("ok"), CancellationToken.None).AsTask());

        _meterListener.RecordObservableInstruments();
        Assert.Equal(0, _allowedCount);
        Assert.Equal(1, _deniedCount);
        Assert.Equal(1, _durationCount);
        Assert.Equal(typeof(TestRequest).FullName, _lastRequestType);
    }

    private class TestRequest : IRequest<string>;

    private class FakeCheck(AuthorizationOutcome outcome) : IRequestAuthorization<TestRequest>
    {
        public ValueTask<AuthorizationOutcome> AuthorizeAsync(TestRequest request, IAuthorizationContext context, CancellationToken cancellationToken)
            => ValueTask.FromResult(outcome);
    }

    private class FakeContextAccessor : IAuthorizationContextAccessor
    {
        public IAuthorizationContext Current { get; } = new AuthorizationContext(null, false, CallerKind.Unknown);
    }
}
