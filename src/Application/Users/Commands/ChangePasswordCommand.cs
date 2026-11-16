using FluentValidation;
using Tempest.Application.Common.Interfaces;
using Tempest.Application.Common.Messaging;
using Tempest.Application.Common.Results;
using Tempest.Application.Common.Security;

namespace Tempest.Application.Users.Commands;

/// <summary>
/// Changes the password of the signed-in user and rotates the security stamp,
/// which signs out every session (including other devices).
/// </summary>
public sealed record ChangePasswordCommand(string CurrentPassword, string NewPassword) : ICommand
{
    public override string ToString() => nameof(ChangePasswordCommand);
}

internal sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty().WithMessage("Ingresa tu contraseña actual.");

        RuleFor(x => x.NewPassword)
            .ValidNewPassword()
            .NotEqual(x => x.CurrentPassword).WithMessage("La nueva contraseña debe ser distinta a la actual.");
    }
}

internal sealed class ChangePasswordCommandHandler(
    ICurrentUser currentUser,
    IUserRepository users,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<ChangePasswordCommand>
{
    public async Task<Result> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return SecurityErrors.AuthenticationRequired;
        }

        var user = await users.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return UserErrors.NotFound;
        }

        if (passwordHasher.Verify(user.PasswordHash, request.CurrentPassword) == PasswordVerification.Failed)
        {
            return Error.Validation(nameof(ChangePasswordCommand.CurrentPassword), "La contraseña actual no es correcta.");
        }

        user.ChangePassword(passwordHasher.Hash(request.NewPassword), timeProvider.GetUtcNow().UtcDateTime);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
