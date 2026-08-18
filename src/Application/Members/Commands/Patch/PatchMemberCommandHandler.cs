using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Shared;
using Domain.Members;
using NodaTime;

namespace Application.Members.Commands.Patch;

/// <summary>
/// Represents the PatchMemberCommandHandler.
/// </summary>
public class PatchMemberCommandHandler(IMemberRepository memberRepository, IClock clock) : ICommandHandler<PatchMemberCommand>
{
    /// <inheritdoc />
    public Task Handle(PatchMemberCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return HandleAsync(request, cancellationToken);
    }

    private async Task HandleAsync(PatchMemberCommand request, CancellationToken cancellationToken)
    {
        var member = await memberRepository.GetByIdAsync(request.Id, cancellationToken)
                     ?? throw new ArgumentException($"Member with id {request.Id} not found");

        if (request.UpdateMask.Contains(nameof(request.IdentifyName), StringComparer.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(request.IdentifyName))
            {
                throw new ArgumentException("IdentifyName cannot be empty when provided in UpdateMask");
            }

            // Check uniqueness
            var existing = await memberRepository.GetByIdentifyNameAsync(request.IdentifyName, cancellationToken);
            if (existing != null && existing.Id != member.Id)
            {
                throw new InvalidOperationException($"IdentifyName '{request.IdentifyName}' is already taken.");
            }

            member.UpdateIdentifyName(request.IdentifyName, clock);
        }

        // We can add other field mask checks here later (e.g. FirstName, LastName)
        bool updateProfile = false;
        string newFirstName = member.FirstName;
        string newLastName = member.LastName;

        if (request.UpdateMask.Contains(nameof(request.FirstName), StringComparer.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(request.FirstName)) throw new ArgumentException("FirstName cannot be empty when provided in UpdateMask");
            newFirstName = request.FirstName;
            updateProfile = true;
        }

        if (request.UpdateMask.Contains(nameof(request.LastName), StringComparer.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(request.LastName)) throw new ArgumentException("LastName cannot be empty when provided in UpdateMask");
            newLastName = request.LastName;
            updateProfile = true;
        }

        if (updateProfile)
        {
            member.Update(
                newFirstName,
                member.MiddleName,
                newLastName,
                member.Description,
                member.PathAvatar ?? string.Empty, // PathAvatar gets preserved inside Update if empty
                member.PreferredTimezone,
                clock
            );
        }

        await memberRepository.UpdateAsync(member, cancellationToken);
        await memberRepository.SaveChangesAsync(cancellationToken);
    }
}
