using System.Threading.Tasks;
using Application.ExternalSync.Commands.EnqueueExternalActivitySync;
using Application.ExternalSync.Commands.EnqueueExternalConnectionRevocation;
using Application.ExternalSync.Commands.ProcessExternalActivitySync;
using Application.ExternalSync.Commands.RequestExternalActivitySync;
using Application.ExternalSync.Commands.RevokeExternalConnectionByExternalUser;
using Application.Members.Commands.DisconnectExternalProvider;
using Application.Shared;
using Domain.Members;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Api.Tests.Features.Application;

public sealed class ApplicationValidationTests(CustomWebApplicationFactory<Program> factory)
    : IClassFixture<CustomWebApplicationFactory<Program>>
{
    [Fact]
    public async Task ExternalSyncValidators_RegisteredApplicationServices_ValidateRequiredFields()
    {
        // Arrange
        await using var scope = factory.Services.CreateAsyncScope();
        var activityEnqueueValidator = scope.ServiceProvider
            .GetRequiredService<IValidator<EnqueueExternalActivitySyncCommand>>();
        var revocationEnqueueValidator = scope.ServiceProvider
            .GetRequiredService<IValidator<EnqueueExternalConnectionRevocationCommand>>();
        var processValidator = scope.ServiceProvider
            .GetRequiredService<IValidator<ProcessExternalActivitySyncCommand>>();
        var requestValidator = scope.ServiceProvider
            .GetRequiredService<IValidator<RequestExternalActivitySyncCommand>>();
        var revokeValidator = scope.ServiceProvider
            .GetRequiredService<IValidator<RevokeExternalConnectionByExternalUserCommand>>();

        // Act
        var activityEnqueueResult = await activityEnqueueValidator.ValidateAsync(
            new EnqueueExternalActivitySyncCommand("", "", "", ""));
        var revocationEnqueueResult = await revocationEnqueueValidator.ValidateAsync(
            new EnqueueExternalConnectionRevocationCommand("", "", ""));
        var processResult = await processValidator.ValidateAsync(
            new ProcessExternalActivitySyncCommand(
                new ExternalActivitySyncJobRequest("", "", "", "idempotency-key")));
        var requestResult = await requestValidator.ValidateAsync(
            new RequestExternalActivitySyncCommand("", ""));
        var revokeResult = await revokeValidator.ValidateAsync(
            new RevokeExternalConnectionByExternalUserCommand("", ""));

        // Assert
        Assert.Equal(4, activityEnqueueResult.Errors.Count);
        Assert.Equal(3, revocationEnqueueResult.Errors.Count);
        Assert.Equal(3, processResult.Errors.Count);
        Assert.Equal(2, requestResult.Errors.Count);
        Assert.Equal(2, revokeResult.Errors.Count);
    }

    [Fact]
    public async Task DisconnectValidator_KnownAndUnknownIdentifiers_ReturnsExpectedResults()
    {
        // Arrange
        await using var scope = factory.Services.CreateAsyncScope();
        var validator = scope.ServiceProvider
            .GetRequiredService<IValidator<DisconnectExternalProviderCommand>>();
        var validCommand = new DisconnectExternalProviderCommand(
            "VALID_MEMBER",
            ExternalProvider.Strava.Id,
            ExternalConnectionType.ActivitiesSync.Id);
        var invalidCommand = new DisconnectExternalProviderCommand(
            new string('X', 101),
            "UNKNOWN_PROVIDER",
            "UNKNOWN_TYPE");

        // Act
        var validResult = await validator.ValidateAsync(validCommand);
        var invalidResult = await validator.ValidateAsync(invalidCommand);

        // Assert
        Assert.True(validResult.IsValid);
        Assert.False(invalidResult.IsValid);
        Assert.Equal(3, invalidResult.Errors.Count);
    }

    [Fact]
    public async Task RevokeExternalConnectionByExternalUser_MissingConnection_ReturnsFalse()
    {
        // Arrange
        await using var scope = factory.Services.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var command = new RevokeExternalConnectionByExternalUserCommand(
            ExternalProvider.Strava.Id,
            "missing-external-user");

        // Act
        var revoked = await sender.Send(command);

        // Assert
        Assert.False(revoked);
    }
}
