using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Tempest.Application.Common.Results;

namespace Tempest.WebUI.Shared.Forms;

/// <summary>
/// Displays Application-layer validation errors (FluentValidation, uniqueness checks…) next to the
/// matching inputs. Field names are the command property names, which the form models mirror.
/// </summary>
public sealed class ServerValidator : ComponentBase, IDisposable
{
    private ValidationMessageStore? _store;
    private EditContext? _context;

    [CascadingParameter]
    private EditContext? CurrentEditContext { get; set; }

    protected override void OnParametersSet()
    {
        if (CurrentEditContext is null)
        {
            throw new InvalidOperationException($"{nameof(ServerValidator)} must be placed inside an EditForm.");
        }

        if (ReferenceEquals(CurrentEditContext, _context))
        {
            return;
        }

        Detach();
        _context = CurrentEditContext;
        _store = new ValidationMessageStore(_context);
        _context.OnValidationRequested += HandleValidationRequested;
        _context.OnFieldChanged += HandleFieldChanged;
    }

    /// <summary>Attaches the field errors of <paramref name="error"/>; returns true when there was at least one.</summary>
    public bool Display(Error error)
    {
        if (_store is null || _context is null)
        {
            return false;
        }

        _store.Clear();
        foreach (var (field, messages) in error.FieldErrors)
        {
            _store.Add(_context.Field(field), messages);
        }

        _context.NotifyValidationStateChanged();
        return error.FieldErrors.Count > 0;
    }

    public void Dispose() => Detach();

    private void HandleValidationRequested(object? sender, ValidationRequestedEventArgs e) => _store?.Clear();

    private void HandleFieldChanged(object? sender, FieldChangedEventArgs e) => _store?.Clear(e.FieldIdentifier);

    private void Detach()
    {
        if (_context is null)
        {
            return;
        }

        _context.OnValidationRequested -= HandleValidationRequested;
        _context.OnFieldChanged -= HandleFieldChanged;
        _context = null;
    }
}
