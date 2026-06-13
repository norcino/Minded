using System;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace MindedExample.Infrastructure.Persistence
{
    /// <summary>
    /// Enforces the application convention that every <see cref="DateTime"/> is UTC.
    /// Values with <see cref="DateTimeKind.Unspecified"/> (typical after JSON deserialization
    /// or test data generation) are interpreted as UTC; local values are converted.
    /// Required by providers that enforce kind discipline, such as PostgreSQL
    /// ('timestamp with time zone'); harmless for SQL Server and SQLite.
    /// </summary>
    public class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
    {
        public UtcDateTimeConverter()
            : base(
                v => v.Kind == DateTimeKind.Local ? v.ToUniversalTime() : DateTime.SpecifyKind(v, DateTimeKind.Utc),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
        { }
    }

    /// <summary>
    /// Nullable counterpart of <see cref="UtcDateTimeConverter"/>.
    /// </summary>
    public class NullableUtcDateTimeConverter : ValueConverter<DateTime?, DateTime?>
    {
        public NullableUtcDateTimeConverter()
            : base(
                v => v.HasValue
                    ? (v.Value.Kind == DateTimeKind.Local ? v.Value.ToUniversalTime() : DateTime.SpecifyKind(v.Value, DateTimeKind.Utc))
                    : v,
                v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v)
        { }
    }
}
