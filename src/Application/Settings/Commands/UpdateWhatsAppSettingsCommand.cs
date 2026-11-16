using FluentValidation;
using Tempest.Application.Common.Interfaces;
using Tempest.Application.Common.Messaging;
using Tempest.Application.Common.Results;
using Tempest.Domain.Entities;
using Tempest.Domain.ValueObjects;

namespace Tempest.Application.Settings.Commands;

public sealed record UpdateWhatsAppSettingsCommand(string PhoneNumber, string MessageTemplate) : ICommand;

internal sealed class UpdateWhatsAppSettingsCommandValidator : AbstractValidator<UpdateWhatsAppSettingsCommand>
{
    public UpdateWhatsAppSettingsCommandValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .Must(phone => WhatsAppNumber.TryCreate(phone, out _))
            .WithMessage(
                $"Ingresa el código de país + número ({WhatsAppNumber.MinDigits} a {WhatsAppNumber.MaxDigits} dígitos), sin el 0 inicial. Ej.: 5215512345678");

        RuleFor(x => x.MessageTemplate)
            .Must(template => Domain.ValueObjects.MessageTemplate.TryCreate(template, out _))
            .WithMessage($"El mensaje es obligatorio y admite hasta {Domain.ValueObjects.MessageTemplate.MaxLength} caracteres.");
    }
}

internal sealed class UpdateWhatsAppSettingsCommandHandler(
    IStoreSettingsRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<UpdateWhatsAppSettingsCommand>
{
    public async Task<Result> Handle(UpdateWhatsAppSettingsCommand request, CancellationToken cancellationToken)
    {
        var settings = await repository.GetAsync(cancellationToken);
        if (settings is null)
        {
            settings = StoreSettings.CreateDefault();
            repository.Add(settings);
        }

        settings.UpdateWhatsApp(
            WhatsAppNumber.Create(request.PhoneNumber),
            Domain.ValueObjects.MessageTemplate.Create(request.MessageTemplate),
            timeProvider.GetUtcNow().UtcDateTime);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
