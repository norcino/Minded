namespace MindedExample.Infrastructure.Persistence
{
    /// <summary>
    /// Provides access to the current request user and tenant identifiers.
    /// </summary>
    public interface ICurrentUserAccessor
    {
        int? UserId { get; }

        int? TenantId { get; }

        bool IsGlobalAdmin { get; }
    }
}
