using System;
using System.Threading;
using System.Threading.Tasks;
using MindedExample.Infrastructure.Persistence;
using Minded.Framework.CQRS.Command;
using MindedExample.Application.User.Command;

namespace MindedExample.Application.User.CommandHandler
{
    /// <summary>
    /// Handler for creating a tenant invitation.
    /// Generates a unique code and token, persists the invite and returns a result that includes the full invite link.
    /// </summary>
    public class CreateTenantInviteCommandHandler : ICommandHandler<CreateTenantInviteCommand, TenantInviteResult>
    {
        private readonly IMindedExampleContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        /// <summary>
        /// Initializes a new <see cref="CreateTenantInviteCommandHandler"/>.
        /// </summary>
        public CreateTenantInviteCommandHandler(IMindedExampleContext context, ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        /// <inheritdoc />
        public async Task<ICommandResponse<TenantInviteResult>> HandleAsync(
            CreateTenantInviteCommand command, CancellationToken cancellationToken = default)
        {
            if (!_currentUserAccessor.TenantId.HasValue || !_currentUserAccessor.UserId.HasValue)
            {
                return new CommandResponse<TenantInviteResult>(null, false);
            }

            var invite = new MindedExample.Domain.TenantInvite
            {
                TenantId = _currentUserAccessor.TenantId.Value,
                CreatedByUserId = _currentUserAccessor.UserId.Value,
                Email = string.IsNullOrWhiteSpace(command.Email)
                    ? null
                    : command.Email.Trim().ToLowerInvariant(),
                Code = GenerateCode(),
                Token = Guid.NewGuid().ToString("N"),
                CreatedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = DateTime.UtcNow.AddDays(7)
            };

            _context.TenantInvites.Add(invite);
            await _context.SaveChangesAsync(cancellationToken);

            var result = new TenantInviteResult
            {
                Id = invite.Id,
                Email = invite.Email,
                Code = invite.Code,
                Token = invite.Token,
                InviteLink = $"{command.FrontendBaseUrl}/register?inviteToken={invite.Token}",
                ExpiresAtUtc = invite.ExpiresAtUtc.ToString("O")
            };

            return new CommandResponse<TenantInviteResult>(result) { Successful = true };
        }

        private static string GenerateCode()
        {
            return Guid.NewGuid().ToString("N").ToUpperInvariant().Substring(0, 8);
        }
    }
}
