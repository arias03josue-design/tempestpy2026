using FluentValidation;
using Tempest.Application.Common.Interfaces;
using Tempest.Application.Common.Messaging;
using Tempest.Application.Common.Results;
using Tempest.Domain.ValueObjects;

namespace Tempest.Application.Users.Commands;

/// <summary>Verifies credentials. Issuing the cookie is a presentation concern (WebUI).</summary>
public sealed record LoginCommand(string Username, string Password) : ICommand<AuthenticatedUserDto>, IAllowAnonymous
{
    // Records print every property by default: keep the password out of logs and debuggers.
    public override string ToString() => $"{nameof(LoginCommand)} {{ Username = {Username} }}";
}

internal sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Username).NotEmpty().WithMessage("Ingresa tu usuario.");
        RuleFor(x => x.Password).NotEmpty().WithMessage("Ingresa tu contraseña.");
    }
}

internal sealed class LoginCommandHandler(
    IUserRepository users,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<LoginCommand, AuthenticatedUserDto>
{
    // Hash verified when the user does not exist, so response time does not reveal valid usernames.
    private static string? _timingEqualizerHash;

    public async Task<Result<AuthenticatedUserDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = Username.TryCreate(request.Username, out var username)
            ? await users.GetByUsernameAsync(username, cancellationToken)
            : null;

        if (user is null)
        {
            _timingEqualizerHash ??= passwordHasher.Hash(Guid.NewGuid().ToString());
            passwordHasher.Verify(_timingEqualizerHash, request.Password);
            return UserErrors.InvalidCredentials;
        }

        var verification = passwordHasher.Verify(user.PasswordHash, request.Password);
        if (verification == PasswordVerification.Failed)
        {
            return UserErrors.InvalidCredentials;
        }

        if (verification == PasswordVerification.SuccessRehashNeeded)
        {
            user.UpgradePasswordHash(passwordHasher.Hash(request.Password));
        }

        user.RecordLogin(timeProvider.GetUtcNow().UtcDateTime);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthenticatedUserDto(user.Id, user.Username.Value, user.SecurityStamp, user.MustChangePassword);
    }
}
