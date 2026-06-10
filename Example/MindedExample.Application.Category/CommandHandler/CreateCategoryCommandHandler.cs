using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using MindedExample.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Minded.Framework.CQRS.Command;
using MindedExample.Application.Category.Command;

namespace MindedExample.Application.Category.CommandHandler
{
    /// <summary>
    /// Handler for creating new categories.
    /// The validator ensures the category data is valid before this handler is called.
    /// If validation fails, this handler will not be executed.
    ///
    /// This handler demonstrates the retry decorator functionality by intentionally failing
    /// the first 3 attempts for each unique category name, then succeeding on the 4th attempt.
    /// This simulates transient failures that can be resolved through retry logic.
    /// </summary>
    public class CreateCategoryCommandHandler : ICommandHandler<CreateCategoryCommand, MindedExample.Domain.Category>
    {
        private readonly IMindedExampleContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;

        /// <summary>
        /// Thread-safe dictionary to track retry attempts per category name.
        /// In a real application, this would be replaced with proper transient error handling.
        /// </summary>
        private static readonly ConcurrentDictionary<string, int> _attemptTracker = new ConcurrentDictionary<string, int>();

        public CreateCategoryCommandHandler(IMindedExampleContext context, ICurrentUserAccessor currentUserAccessor)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
        }

        /// <summary>
        /// Creates a new category in the database.
        /// Uses async AddAsync and SaveChangesAsync for proper async/await pattern.
        ///
        /// DEMO: This handler intentionally fails the first 3 attempts to demonstrate retry logic.
        /// In production, remove the retry simulation logic and handle actual transient errors.
        /// </summary>
        /// <param name="command">The create command containing the category to create</param>
        /// <param name="cancellationToken">Cancellation token for cooperative cancellation</param>
        /// <returns>Successful command response with the created category</returns>
        /// <exception cref="InvalidOperationException">Thrown on first 3 attempts to simulate transient failure</exception>
        public async Task<ICommandResponse<MindedExample.Domain.Category>> HandleAsync(CreateCategoryCommand command, CancellationToken cancellationToken = default)
        {
            if (!_currentUserAccessor.TenantId.HasValue)
            {
                return new CommandResponse<MindedExample.Domain.Category>(default(MindedExample.Domain.Category), false);
            }

            var userExistsInTenant = await _context.Users.AnyAsync(
                u => u.Id == command.Category.UserId && u.TenantId == _currentUserAccessor.TenantId.Value,
                cancellationToken);

            if (!userExistsInTenant)
            {
                return new CommandResponse<MindedExample.Domain.Category>(default(MindedExample.Domain.Category), false);
            }

            // DEMO: Simulate transient failures for the first 3 attempts
            // This demonstrates the retry decorator in action
            var categoryKey = $"{command.Category.Name}_{command.TraceId}";
            var attemptCount = _attemptTracker.AddOrUpdate(categoryKey, 1, (key, count) => count + 1);

            if (attemptCount < 4)
            {
                throw new InvalidOperationException(
                    $"Simulated transient failure for category '{command.Category.Name}' (Attempt {attemptCount}/3). " +
                    $"The retry decorator will automatically retry this operation.");
            }

            // After 3 failures, this attempt (4th) will succeed
            await _context.Categories.AddAsync(command.Category, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            // Clean up the tracker after successful completion
            _attemptTracker.TryRemove(categoryKey, out _);

            return new CommandResponse<MindedExample.Domain.Category>(command.Category)
            {
                Successful = true
            };
        }
    }
}
