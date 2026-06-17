using System;
using Minded.Extensions.Authorization.Attributes;
using Minded.Extensions.Logging;
using Minded.Extensions.Transaction.Decorator;
using Minded.Extensions.Validation.Decorator;
using Minded.Framework.CQRS.Command;

namespace MindedExample.Application.Configuration.Command
{
    /// <summary>
    /// Command to create a new tenant with its legal owner account.
    /// Requires the caller to be a global administrator.
    /// Uses [TransactionalCommand] because the handler creates multiple entities (Tenant, User,
    /// role-permission rows) across multiple SaveChangesAsync calls — all must succeed or all roll back.
    /// </summary>
    [ValidateCommand]
    [TransactionalCommand]
    [RequireClaim("is_global_admin", "true")]
    public class CreateTenantCommand : ICommand<CreateTenantResult>, ILoggable
    {
        public CreateTenantCommand(
            string name,
            string legalOwnerName,
            string legalOwnerSurname,
            string legalOwnerEmail,
            string legalOwnerPassword,
            Guid? traceId = null)
        {
            Name = name;
            LegalOwnerName = legalOwnerName;
            LegalOwnerSurname = legalOwnerSurname;
            LegalOwnerEmail = legalOwnerEmail;
            LegalOwnerPassword = legalOwnerPassword;
            TraceId = traceId ?? TraceId;
        }

        public string Name { get; }

        public string LegalOwnerName { get; }

        public string LegalOwnerSurname { get; }

        public string LegalOwnerEmail { get; }

        public string LegalOwnerPassword { get; }

        public Guid TraceId { get; } = Guid.NewGuid();

        public string LoggingTemplate => "Creating tenant {TenantName} with owner {LegalOwnerEmail}";

        public string[] LoggingProperties => new[] { nameof(Name), nameof(LegalOwnerEmail) };
    }

    /// <summary>
    /// Result payload for tenant creation.
    /// </summary>
    public class CreateTenantResult
    {
        public int Id { get; set; }

        public string Name { get; set; }
    }
}
