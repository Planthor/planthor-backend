using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Dtos;
using Application.ExternalSync.Commands.SyncStravaActivities;
using Application.Interfaces;
using Domain.Members;
using Domain.Plans;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NodaTime;

namespace Application.Tests.ExternalSync.Commands.SyncStravaActivities;

public class SyncStravaActivitiesCommandHandlerTests
{
    private readonly IMemberRepository _memberRepositoryMock;
    private readonly IPlanRepository _planRepositoryMock;
    private readonly IServiceProvider _serviceProviderMock;
    private readonly IKeyedServiceProvider _keyedServiceProviderMock;
    private readonly IClock _clockMock;
    private readonly IActivitySyncAdapter _stravaAdapterMock;
    private readonly IProviderSportTypeMapper _sportTypeMapperMock;
    private readonly SyncStravaActivitiesCommandHandler _handler;

    public SyncStravaActivitiesCommandHandlerTests()
    {
        _memberRepositoryMock = Substitute.For<IMemberRepository>();
        _planRepositoryMock = Substitute.For<IPlanRepository>();
        
        _serviceProviderMock = Substitute.For<IServiceProvider, IKeyedServiceProvider>();
        _keyedServiceProviderMock = (IKeyedServiceProvider)_serviceProviderMock;
        
        _clockMock = Substitute.For<IClock>();
        _stravaAdapterMock = Substitute.For<IActivitySyncAdapter>();
        _sportTypeMapperMock = Substitute.For<IProviderSportTypeMapper>();

        _serviceProviderMock.GetService(typeof(IActivitySyncAdapter))
            .Returns(_stravaAdapterMock);
        _serviceProviderMock.GetService(typeof(IProviderSportTypeMapper))
            .Returns(_sportTypeMapperMock);
            
        _keyedServiceProviderMock.GetRequiredKeyedService(typeof(IActivitySyncAdapter), "STRAVA")
            .Returns(_stravaAdapterMock);
        _keyedServiceProviderMock.GetRequiredKeyedService(typeof(IProviderSportTypeMapper), "STRAVA")
            .Returns(_sportTypeMapperMock);

        _handler = new SyncStravaActivitiesCommandHandler(
            _memberRepositoryMock,
            _planRepositoryMock,
            _serviceProviderMock,
            _clockMock);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenMemberRepositoryIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new SyncStravaActivitiesCommandHandler(null!, _planRepositoryMock, _serviceProviderMock, _clockMock));
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenPlanRepositoryIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new SyncStravaActivitiesCommandHandler(_memberRepositoryMock, null!, _serviceProviderMock, _clockMock));
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenServiceProviderIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new SyncStravaActivitiesCommandHandler(_memberRepositoryMock, _planRepositoryMock, null!, _clockMock));
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenClockIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new SyncStravaActivitiesCommandHandler(_memberRepositoryMock, _planRepositoryMock, _serviceProviderMock, null!));
    }

    [Fact]
    public async Task Handle_ShouldThrowArgumentNullException_WhenRequestIsNull()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _handler.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ShouldThrowArgumentException_WhenMemberNotFound()
    {
        var command = new SyncStravaActivitiesCommand("user1");
        _memberRepositoryMock.GetByIdentifyNameAsync("user1", Arg.Any<CancellationToken>())
            .Returns((Member?)null);

        await Assert.ThrowsAsync<ArgumentException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ShouldThrowInvalidOperationException_WhenNoActiveStravaConnection()
    {
        var command = new SyncStravaActivitiesCommand("user1");
        var member = Member.Create("user1", "John", "Doe", "Smith", "desc", "UTC", SystemClock.Instance);
        _memberRepositoryMock.GetByIdentifyNameAsync("user1", Arg.Any<CancellationToken>())
            .Returns(member);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ShouldReturnZero_WhenNoLinkedPlans()
    {
        var command = new SyncStravaActivitiesCommand("user1");
        var clock = SystemClock.Instance;
        var member = Member.Create("user1", "John", "Doe", "Smith", "desc", "UTC", clock);
        member.ConnectExternalProvider(ExternalProvider.Strava, ExternalConnectionType.ActivitiesSync, "ext1", ["read"], clock);

        _memberRepositoryMock.GetByIdentifyNameAsync("user1", Arg.Any<CancellationToken>())
            .Returns(member);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(0, result);
    }

    [Fact]
    public async Task Handle_ShouldSkip_WhenActivitiesEmpty()
    {
        var command = new SyncStravaActivitiesCommand("user1");
        var clock = SystemClock.Instance;
        var member = Member.Create("user1", "John", "Doe", "Smith", "desc", "UTC", clock);
        member.ConnectExternalProvider(ExternalProvider.Strava, ExternalConnectionType.ActivitiesSync, "ext1", ["read"], clock);
        _memberRepositoryMock.GetByIdentifyNameAsync("user1", Arg.Any<CancellationToken>())
            .Returns(member);
        
        var planId = Guid.NewGuid();
        member.SubscribeToPlan(planId, true, 0, true, clock);
        
        _stravaAdapterMock.FetchActivitiesAsync(member.Id, "user1", null, Arg.Any<CancellationToken>())
            .Returns([]);

        var result = await _handler.Handle(command, CancellationToken.None);
        Assert.Equal(0, result);
    }
    
    [Fact]
    public async Task Handle_ShouldSkipPlan_WhenPlanIsNull()
    {
        var command = new SyncStravaActivitiesCommand("user1");
        var clock = SystemClock.Instance;
        var member = Member.Create("user1", "John", "Doe", "Smith", "desc", "UTC", clock);
        member.ConnectExternalProvider(ExternalProvider.Strava, ExternalConnectionType.ActivitiesSync, "ext1", ["read"], clock);
        _memberRepositoryMock.GetByIdentifyNameAsync("user1", Arg.Any<CancellationToken>())
            .Returns(member);
        
        var planId = Guid.NewGuid();
        member.SubscribeToPlan(planId, true, 0, true, clock);
        
        _planRepositoryMock.GetByIdAsync(planId, Arg.Any<CancellationToken>())
            .Returns((Plan?)null);
            
        _stravaAdapterMock.FetchActivitiesAsync(member.Id, "user1", null, Arg.Any<CancellationToken>())
            .Returns(
            [
                new AdapterActivityDto("ext1", "STRAVA", "Run", Instant.FromUtc(2026, 1, 1, 0, 0), "Run", 10000, null)
            ]);

        var result = await _handler.Handle(command, CancellationToken.None);
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task Handle_WithActivities_AndLinkedPlans_ProcessesActivities()
    {
        var command = new SyncStravaActivitiesCommand("user1");
        var clock = SystemClock.Instance;
        var member = Member.Create("user1", "John", "Doe", "Smith", "desc", "UTC", clock);
        member.ConnectExternalProvider(ExternalProvider.Strava, ExternalConnectionType.ActivitiesSync, "ext1", ["read"], clock);
        
        var planId = Guid.NewGuid();
        member.SubscribeToPlan(planId, true, 0, true, clock);

        _memberRepositoryMock.GetByIdentifyNameAsync("user1", Arg.Any<CancellationToken>())
            .Returns(member);

        var sportPlanDetails = new SportPlanDetails("km", ["Run", "ALL"]);
        var plan = Plan.CreateSportPlan("Plan", "km", 100, Instant.FromUtc(2025, 1, 1, 0, 0), Instant.FromUtc(2027, 1, 1, 0, 0), "2025-01-01", "2027-01-01", "UTC", true, sportPlanDetails, clock, member.Id);
        typeof(Plan).GetProperty("Id")!.SetValue(plan, planId);
        
        // Add log to test skip by external id
        plan.AddActivityLog(10f, "2026-01-01", new ExternalActivitySource(ExternalProvider.Strava, "extSkip"), clock, member.Id);
        
        _planRepositoryMock.GetByIdAsync(planId, Arg.Any<CancellationToken>())
            .Returns(plan);
            
        _stravaAdapterMock.FetchActivitiesAsync(member.Id, "user1", null, Arg.Any<CancellationToken>())
            .Returns(
            [
                new AdapterActivityDto("ext1", "STRAVA", "Run", Instant.FromUtc(2026, 1, 1, 0, 0), "Run", 10000, null),
                new AdapterActivityDto("ext2", "STRAVA", "Run", Instant.FromUtc(2024, 1, 1, 0, 0), "Run", 5000, null), // Out of bounds
                new AdapterActivityDto("extSkip", "STRAVA", "Run", Instant.FromUtc(2026, 1, 1, 0, 0), "Run", 10000, null), // Already exists
                new AdapterActivityDto("extNullDist", "STRAVA", "Run", Instant.FromUtc(2026, 1, 1, 0, 0), "Run", null, null), // Null distance
                new AdapterActivityDto("extNoType", "STRAVA", "Unknown", Instant.FromUtc(2026, 1, 1, 0, 0), "Unknown", 10000, null) // Unknown type
            ]);

        _sportTypeMapperMock.MapToPlanthor("Run").Returns(PlanthorSportType.Run);
        _sportTypeMapperMock.MapToPlanthor("Unknown").Returns((PlanthorSportType?)null);

        var result = await _handler.Handle(command, CancellationToken.None);
        Assert.Equal(3, result); // ext1, extNullDist (0 dist), extNoType (isAll = true)
        
        // Let's test with miles too
        plan.Update("mi", 100, 0, Instant.FromUtc(2025, 1, 1, 0, 0), Instant.FromUtc(2027, 1, 1, 0, 0), member.Id, clock);
        _stravaAdapterMock.FetchActivitiesAsync(member.Id, "user1", null, Arg.Any<CancellationToken>())
            .Returns([new AdapterActivityDto("ext3", "STRAVA", "Run", Instant.FromUtc(2026, 1, 2, 0, 0), "Run", 1609.34, null)]);
        var result2 = await _handler.Handle(command, CancellationToken.None);
        Assert.Equal(1, result2);
        
        // Let's test with m too
        plan.Update("m", 100, 0, Instant.FromUtc(2025, 1, 1, 0, 0), Instant.FromUtc(2027, 1, 1, 0, 0), member.Id, clock);
        _stravaAdapterMock.FetchActivitiesAsync(member.Id, "user1", null, Arg.Any<CancellationToken>())
            .Returns([new AdapterActivityDto("ext4", "STRAVA", "Run", Instant.FromUtc(2026, 1, 2, 0, 0), "Run", 100, null)]);
        var result3 = await _handler.Handle(command, CancellationToken.None);
        Assert.Equal(1, result3);
        
        // Let's test with yd too
        plan.Update("yd", 100, 0, Instant.FromUtc(2025, 1, 1, 0, 0), Instant.FromUtc(2027, 1, 1, 0, 0), member.Id, clock);
        _stravaAdapterMock.FetchActivitiesAsync(member.Id, "user1", null, Arg.Any<CancellationToken>())
            .Returns([new AdapterActivityDto("ext5", "STRAVA", "Run", Instant.FromUtc(2026, 1, 2, 0, 0), "Run", 91.44, null)]);
        var result4 = await _handler.Handle(command, CancellationToken.None);
        Assert.Equal(1, result4);
    }

    [Fact]
    public async Task Handle_ShouldSkipPlan_WhenSportPlanDetailsIsNull()
    {
        var command = new SyncStravaActivitiesCommand("user1");
        var clock = SystemClock.Instance;
        var member = Member.Create("user1", "John", "Doe", "Smith", "desc", "UTC", clock);
        member.ConnectExternalProvider(ExternalProvider.Strava, ExternalConnectionType.ActivitiesSync, "ext1", ["read"], clock);
        
        var planId = Guid.NewGuid();
        member.SubscribeToPlan(planId, true, 0, true, clock);

        _memberRepositoryMock.GetByIdentifyNameAsync("user1", Arg.Any<CancellationToken>())
            .Returns(member);

        // Create ordinary Plan without SportPlanDetails
        var plan = Plan.Create("Plan", "km", 100, Instant.FromUtc(2025, 1, 1, 0, 0), Instant.FromUtc(2027, 1, 1, 0, 0), "2025-01-01", "2027-01-01", "UTC", true, clock, member.Id);
        typeof(Plan).GetProperty("Id")!.SetValue(plan, planId);
        
        _planRepositoryMock.GetByIdAsync(planId, Arg.Any<CancellationToken>())
            .Returns(plan);
            
        _stravaAdapterMock.FetchActivitiesAsync(member.Id, "user1", null, Arg.Any<CancellationToken>())
            .Returns(
            [
                new AdapterActivityDto("ext1", "STRAVA", "Run", Instant.FromUtc(2026, 1, 1, 0, 0), "Run", 10000, null)
            ]);

        _sportTypeMapperMock.MapToPlanthor("Run").Returns(PlanthorSportType.Run);

        var result = await _handler.Handle(command, CancellationToken.None);
        Assert.Equal(0, result);
    }
}
