using Tempest.Application.Common.Messaging;
using Tempest.Application.Common.Results;
using Tempest.Application.Common.Security;

namespace Tempest.Application.Common.Behaviors;

/// <summary>
/// Secure by default: every request requires an authenticated caller unless it implements
/// <see cref="IAllowAnonymous"/>. Defense in depth on top of the <c>[Authorize]</c> pages.
/// </summary>
internal sealed class AuthorizationBehavior<TRequest, TResponse>(ICurrentUser currentUser)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : IResultFactory<TResponse>
{
    private static readonly bool AllowsAnonymous = typeof(IAllowAnonymous).IsAssignableFrom(typeof(TRequest));

    public Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken) =>
        AllowsAnonymous || currentUser.IsAuthenticated
            ? next()
            : Task.FromResult(TResponse.FromError(SecurityErrors.AuthenticationRequired));
}
