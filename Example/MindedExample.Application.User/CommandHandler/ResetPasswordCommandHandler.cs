using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Minded.Extensions.Exception;
using Minded.Framework.CQRS.Abstractions;
using Minded.Framework.CQRS.Command;
using MindedExample.Application.Common;
using MindedExample.Application.User.Command;
using MindedExample.Infrastructure.Persistence;

namespace MindedExample.Application.User.CommandHandler
{
    /// <summary>
    /// Handles <see cref="ResetPasswordCommand"/> by locating the token, verifying it has not
    /// expired or already been used, updating the user's password hash, and marking the token
    /// as consumed. Returns a validation error (HTTP 400) when the token is invalid.
    /// </summary>
    public class ResetPasswordCommandHandler : ICommandHandler<ResetPasswordCommand>
    {
        private readonly IMindedExampleContext _context;
        private readonly IPasswordService _passwordService;

        /// <summary>
        /// Initializes a new <see cref="ResetPasswordCommandHandler"/>.
        /// </summary>
        public ResetPasswordCommandHandler(IMindedExampleContext context, IPasswordService passwordService)
        {
            _context = context;
            _passwordService = passwordService;
        }

        /// <inheritdoc />
        public async Task<ICommandResponse> HandleAsync(ResetPasswordCommand command, CancellationToken cancellationToken = default)
        {
            var tokenRecord = await _context.PasswordResetTokens
                .Include(t => t.User)
                .SingleOrDefaultAsync(t => t.Token == command.Token, cancellationToken);

            if (tokenRecord == null || tokenRecord.UsedAtUtc != null || tokenRecord.ExpiresAtUtc < DateTime.UtcNow)
            {
                return CommandResponse.Error(
                    new OutcomeEntry(
                        nameof(command.Token),
                        "{0} is invalid or expired",
                        attemptedValue: null,
                        Severity.Error,
                        GenericErrorCodes.ValidationFailed));
            }

            tokenRecord.User.PasswordHash = _passwordService.HashPassword(tokenRecord.User, command.NewPassword);
            tokenRecord.UsedAtUtc = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            return CommandResponse.Success();
        }
    }
}
