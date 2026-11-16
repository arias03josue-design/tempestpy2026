using FluentValidation;
using Tempest.Application.Common.Interfaces;
using Tempest.Application.Common.Messaging;
using Tempest.Application.Common.Results;
using Tempest.Domain.Entities;
using Tempest.Domain.ValueObjects;

namespace Tempest.Application.Users.Commands;

/// <summary>Creates another administrator. Their temporary password must be changed on first use.</summary>
public sealed record CreateUserCommand(string Username, string Password) : ICommand<Guid>
{
    public override string ToString() => $"{nameof(CreateUserCommand)} {{ Username = {Username} }}";
}

internal sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Username)
            .Must(name => Domain.ValueObjects.Username.TryCreate(name, out _))
            .WithMessage(
                $"Usa entre {Domain.ValueObjects.Username.MinLength} y {Domain.ValueObjects.Username.MaxLength} caracteres: letras sin acentos, números, punto, guion o guion bajo.");

        RuleFor(x => x.Password).ValidNewPassword();
    }
}

internal sealed class CreateUserCommandHandler(
    IUserRepository users,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<CreateUserCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var username = Domain.ValueObjects.Username.Create(request.Username);
        if (await users.ExistsAsync(username, cancellationToken))
        {
            return Error.Validation(nameof(CreateUserCommand.Username), "Ya existe un administrador con ese usuario.");
        }

        var user = User.Create(
            username,
            passwordHasher.Hash(request.Password),
            mustChangePassword: true,
            timeProvider.GetUtcNow().UtcDateTime);

        users.Add(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return user.Id;
    }
}
