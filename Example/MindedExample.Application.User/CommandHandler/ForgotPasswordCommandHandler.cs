using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Minded.Framework.CQRS.Command;
using MindedExample.Application.User.Command;
using MindedExample.Domain;
using MindedExample.Infrastructure.Persistence;

namespace MindedExample.Application.User.CommandHandler
{
    /// <summary>
    /// Handles <see cref="ForgotPasswordCommand"/> by creating a one-time password reset token
    /// and logging it. Always returns success regardless of whether the email is registered —
    /// this prevents account enumeration attacks.
    /// In a production system the token would be sent via email rather than logged.
    /// </summary>
    public class ForgotPasswordCommandHandler : ICommandHandler<ForgotPasswordCommand>
    {
        private readonly IMindedExampleContext _context;
        private readonly ILogger<ForgotPasswordCommandHandler> _logger;

        /// <summary>
        /// Initializes a new <see cref="ForgotPasswordCommandHandler"/>.
        /// </summary>
        public ForgotPasswordCommandHandler(IMindedExampleContext context, ILogger<ForgotPasswordCommandHandler> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<ICommandResponse> HandleAsync(ForgotPasswordCommand command, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(command.Email))
                return CommandResponse.Success();

            var email = command.Email.Trim().ToLowerInvariant();
            var user = await _context.Users
                .SingleOrDefaultAsync(u => u.Email == email && u.IsActive, cancellationToken);

            if (user == null)
                return CommandResponse.Success();

            var token = Guid.NewGuid().ToString("N");
            _context.PasswordResetTokens.Add(new PasswordResetToken
            {
                UserId = user.Id,
                Token = token,
                CreatedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = DateTime.UtcNow.AddHours(2)
            });

            await _context.SaveChangesAsync(cancellationToken);

            // In a real application, the reset link would be sent by email.
            // For the example app the token is logged so E2E tests can read it from the DB.
            _logger.LogInformation("Password reset token for {Email}: {Token}", user.Email, token);

            return CommandResponse.Success();
        }
    }
}
