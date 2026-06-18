using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Minded.Framework.CQRS.Query;
using MindedExample.Application.User.Command;
using MindedExample.Application.User.Query;
using MindedExample.Application.User.Services;
using MindedExample.Infrastructure.Persistence;

namespace MindedExample.Application.User.QueryHandler
{
    /// <summary>
    /// Handles <see cref="GetCurrentUserQuery"/> by looking up the authenticated user
    /// in the database and building a fully-populated <see cref="AuthResult"/> via
    /// <see cref="IAuthResultBuilder"/>. Returns <c>null</c> when the user cannot be found,
    /// which causes the REST mediator to respond with 404 Not Found.
    /// </summary>
    public class GetCurrentUserQueryHandler : IQueryHandler<GetCurrentUserQuery, AuthResult>
    {
        private readonly IMindedExampleContext _context;
        private readonly ICurrentUserAccessor _currentUserAccessor;
        private readonly IAuthResultBuilder _authResultBuilder;

        /// <summary>
        /// Initializes a new <see cref="GetCurrentUserQueryHandler"/>.
        /// </summary>
        public GetCurrentUserQueryHandler(
            IMindedExampleContext context,
            ICurrentUserAccessor currentUserAccessor,
            IAuthResultBuilder authResultBuilder)
        {
            _context = context;
            _currentUserAccessor = currentUserAccessor;
            _authResultBuilder = authResultBuilder;
        }

        /// <inheritdoc />
        public async Task<AuthResult> HandleAsync(GetCurrentUserQuery query, CancellationToken cancellationToken = default)
        {
            if (!_currentUserAccessor.UserId.HasValue)
                return null;

            var user = await _context.Users
                .SingleOrDefaultAsync(u => u.Id == _currentUserAccessor.UserId.Value, cancellationToken);

            if (user == null)
                return null;

            return await _authResultBuilder.BuildAsync(user, accessToken: null, cancellationToken);
        }
    }
}
