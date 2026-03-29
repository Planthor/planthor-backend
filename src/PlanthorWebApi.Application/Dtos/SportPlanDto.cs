using System;
using System.Collections.Generic;

namespace PlanthorWebApi.Application.Dtos;

/// <summary>
/// Data transfer object representing a trackable sport plan owned by a member.
/// </summary>
/// <param name="Id">The unique identifier of the plan.</param>
/// <param name="MemberId">The identifier of the member who owns this plan.</param>
/// <param name="Name">The display name of this plan.</param>
/// <param name="Unit">The unit of measurement for this plan's target and activity logs.</param>
/// <param name="Target">The numeric target this plan aims to reach.</param>
/// <param name="CurrentValue">The current aggregate value across all activity logs.</param>
/// <param name="From">The UTC date and time at which this period starts.</param>
/// <param name="To">The UTC date and time at which this period ends.</param>
/// <param name="StartDateLocal">The local start date as an ISO string.</param>
/// <param name="EndDateLocal">The local end date as an ISO string.</param>
/// <param name="Timezone">The IANA timezone identifier snapshotted at creation time.</param>
/// <param name="Completed">Indicates whether this plan has been completed.</param>
/// <param name="EnableActivityLog">Indicates whether activity logging is enabled for this plan.</param>
/// <param name="StatusI18nKey">The i18n localization key for the current lifecycle status of this plan.</param>
/// <param name="LikeCount">The total number of likes on this plan.</param>
/// <param name="SportTypes">The list of accepted Strava sport type identifiers.</param>
public record SportPlanDto(
    Guid Id,
    Guid MemberId,
    string Name,
    string Unit,
    float Target,
    float CurrentValue,
    DateTimeOffset From,
    DateTimeOffset To,
    string StartDateLocal,
    string EndDateLocal,
    string Timezone,
    bool Completed,
    bool EnableActivityLog,
    string StatusI18nKey,
    int LikeCount,
    IReadOnlyList<string> SportTypes
);
