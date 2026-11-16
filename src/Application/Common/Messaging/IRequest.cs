using Tempest.Application.Common.Results;

namespace Tempest.Application.Common.Messaging;

/// <summary>Any message dispatched through <see cref="ISender"/>.</summary>
public interface IRequest<TResponse>
{
}

/// <summary>State-changing use case without payload.</summary>
public interface ICommand : IRequest<Result>
{
}

/// <summary>State-changing use case that returns a value.</summary>
public interface ICommand<TValue> : IRequest<Result<TValue>>
{
}

/// <summary>Read-only use case.</summary>
public interface IQuery<TValue> : IRequest<Result<TValue>>
{
}

/// <summary>
/// Requests are protected by default (secure by default). Implement this marker on the few
/// use cases a visitor may run without signing in.
/// </summary>
public interface IAllowAnonymous
{
}
