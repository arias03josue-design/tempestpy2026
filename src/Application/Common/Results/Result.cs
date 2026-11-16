using System.Diagnostics.CodeAnalysis;

namespace Tempest.Application.Common.Results;

/// <summary>
/// Lets pipeline behaviors (validation, authorization…) build a failed response
/// without knowing the concrete result type of the request.
/// </summary>
public interface IResultFactory<TSelf>
    where TSelf : IResultFactory<TSelf>
{
    static abstract TSelf FromError(Error error);
}

/// <summary>Outcome of a command without payload.</summary>
public class Result : IResultFactory<Result>
{
    protected Result(Error? error) => Error = error;

    public Error? Error { get; }

    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess => Error is null;

    [MemberNotNullWhen(true, nameof(Error))]
    public bool IsFailure => Error is not null;

    public static Result Success() => new(null);

    public static Result Failure(Error error) => new(error ?? throw new ArgumentNullException(nameof(error)));

    public static Result<TValue> Success<TValue>(TValue value) => Result<TValue>.Success(value);

    public static implicit operator Result(Error error) => Failure(error);

    static Result IResultFactory<Result>.FromError(Error error) => Failure(error);
}

/// <summary>Outcome of a query or a command that returns a value.</summary>
public sealed class Result<TValue> : Result, IResultFactory<Result<TValue>>
{
    private readonly TValue? _value;

    private Result(TValue value)
        : base(null) => _value = value;

    private Result(Error error)
        : base(error)
    {
    }

    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException($"Cannot read the value of a failed result ({Error.Code}).");

    public static Result<TValue> Success(TValue value) => new(value);

    public static new Result<TValue> Failure(Error error) => new(error ?? throw new ArgumentNullException(nameof(error)));

    public static implicit operator Result<TValue>(TValue value) => Success(value);

    public static implicit operator Result<TValue>(Error error) => Failure(error);

    static Result<TValue> IResultFactory<Result<TValue>>.FromError(Error error) => Failure(error);
}
