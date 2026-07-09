using System;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NodaTime;

namespace Infrastructure.Context;

/// <summary>
/// Converts a NodaTime Instant to a UTC DateTime for Entity Framework Core.
/// </summary>
public class InstantToDateTimeUtcConverter : ValueConverter<Instant, DateTime>
{
    public InstantToDateTimeUtcConverter()
        : base(
            instant => instant.ToDateTimeUtc(),
            dateTime => Instant.FromDateTimeUtc(DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)),
            null)
    {
    }
}
