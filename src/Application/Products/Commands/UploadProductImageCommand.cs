using FluentValidation;
using Tempest.Application.Common.Files;
using Tempest.Application.Common.Interfaces;
using Tempest.Application.Common.Messaging;
using Tempest.Application.Common.Results;

namespace Tempest.Application.Products.Commands;

/// <summary>Stores an image uploaded from the admin panel and returns its public URL.</summary>
public sealed record UploadProductImageCommand(Stream Content, string FileName) : ICommand<string>
{
    public const long MaxBytes = 5 * 1024 * 1024;
}

internal sealed class UploadProductImageCommandValidator : AbstractValidator<UploadProductImageCommand>
{
    public UploadProductImageCommandValidator()
    {
        RuleFor(x => x.Content).NotNull().Must(stream => stream.CanRead).WithMessage("No se pudo leer el archivo.");
        RuleFor(x => x.FileName).NotEmpty().WithMessage("El archivo no tiene nombre.");
    }
}

internal sealed class UploadProductImageCommandHandler(IImageStorage imageStorage)
    : ICommandHandler<UploadProductImageCommand, string>
{
    private const string Field = nameof(UploadProductImageCommand.Content);

    public async Task<Result<string>> Handle(UploadProductImageCommand request, CancellationToken cancellationToken)
    {
        // Read with a hard limit: never trust the size reported by the client.
        using var buffer = new MemoryStream();
        var chunk = new byte[81_920];
        int read;
        while ((read = await request.Content.ReadAsync(chunk.AsMemory(), cancellationToken)) > 0)
        {
            if (buffer.Length + read > UploadProductImageCommand.MaxBytes)
            {
                return Error.Validation(Field, "La imagen supera el máximo de 5 MB.");
            }

            buffer.Write(chunk, 0, read);
        }

        if (buffer.Length == 0)
        {
            return Error.Validation(Field, "El archivo está vacío.");
        }

        var content = buffer.GetBuffer().AsMemory(0, (int)buffer.Length);
        var format = ImageFormat.Detect(content.Span);
        if (format is null)
        {
            return Error.Validation(Field, "Formato no soportado. Usa una imagen JPG, PNG, WEBP o GIF.");
        }

        return await imageStorage.SaveAsync(content, format.Extension, cancellationToken);
    }
}
