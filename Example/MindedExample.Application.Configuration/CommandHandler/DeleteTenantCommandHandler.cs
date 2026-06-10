using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Minded.Framework.CQRS.Command;
using MindedExample.Application.Configuration.Command;
using MindedExample.Infrastructure.Persistence;

namespace MindedExample.Application.Configuration.CommandHandler
{
    /// <summary>
    /// Handles tenant deletion with full tenant-scoped data cleanup.
    /// </summary>
    public class DeleteTenantCommandHandler : ICommandHandler<DeleteTenantCommand>
    {
        private readonly IMindedExampleContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        public DeleteTenantCommandHandler(IMindedExampleContext context, ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        public async Task<ICommandResponse> HandleAsync(DeleteTenantCommand command, CancellationToken cancellationToken = default)
        {
            if (!_currentUserAccessor.IsGlobalAdmin)
            {
                throw new SecurityException();
            }

            var tenant = await _context.Tenants
                .AsNoTracking()
                .SingleOrDefaultAsync(t => t.Id == command.TenantId, cancellationToken);

            if (tenant == null)
            {
                return new CommandResponse
                {
                    Successful = false
                };
            }

            var tenantUserIds = await _context.Users
                .AsNoTracking()
                .Where(u => u.TenantId == command.TenantId)
                .Select(u => u.Id)
                .ToListAsync(cancellationToken);

            if (_context is MindedExampleContext concreteContext)
            {
                await concreteContext.Database.ExecuteSqlRawAsync("DELETE FROM UserRoles WHERE TenantId = {0}", command.TenantId);
                await concreteContext.Database.ExecuteSqlRawAsync("DELETE FROM RolePermissions WHERE TenantId = {0}", command.TenantId);
            }

            await _context.TenantInvites.Where(i => i.TenantId == command.TenantId).ExecuteDeleteAsync(cancellationToken);
            await _context.TenantJoinRequests.Where(r => r.TenantId == command.TenantId).ExecuteDeleteAsync(cancellationToken);
            await _context.Transactions.Where(t => t.User.TenantId == command.TenantId).ExecuteDeleteAsync(cancellationToken);
            await _context.Categories.Where(c => c.User.TenantId == command.TenantId).ExecuteDeleteAsync(cancellationToken);

            if (tenantUserIds.Count > 0)
            {
                await _context.PasswordResetTokens.Where(t => tenantUserIds.Contains(t.UserId)).ExecuteDeleteAsync(cancellationToken);
                await _context.Users.Where(u => tenantUserIds.Contains(u.Id)).ExecuteDeleteAsync(cancellationToken);
            }

            await _context.Tenants.Where(t => t.Id == command.TenantId).ExecuteDeleteAsync(cancellationToken);

            return CommandResponse.Success();
        }
    }

}

