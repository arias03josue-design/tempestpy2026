namespace Tempest.WebUI.Shared.UI;

public enum ToastKind
{
    Success,
    Error,
    Info,
}

public sealed record Toast(Guid Id, ToastKind Kind, string Message);

/// <summary>Per-circuit notification queue rendered by <see cref="ToastHost"/>.</summary>
public sealed class ToastService
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromSeconds(4.5);

    private readonly List<Toast> _items = [];
    private readonly Lock _gate = new();

    public event Action? Changed;

    public IReadOnlyList<Toast> Items
    {
        get
        {
            lock (_gate)
            {
                return [.. _items];
            }
        }
    }

    public void Success(string message) => Show(ToastKind.Success, message);

    public void Error(string message) => Show(ToastKind.Error, message);

    public void Info(string message) => Show(ToastKind.Info, message);

    public void Dismiss(Guid id)
    {
        bool removed;
        lock (_gate)
        {
            removed = _items.RemoveAll(toast => toast.Id == id) > 0;
        }

        if (removed)
        {
            Changed?.Invoke();
        }
    }

    private void Show(ToastKind kind, string message)
    {
        var toast = new Toast(Guid.NewGuid(), kind, message);
        lock (_gate)
        {
            _items.Add(toast);
        }

        Changed?.Invoke();
        _ = DismissLaterAsync(toast.Id);
    }

    private async Task DismissLaterAsync(Guid id)
    {
        await Task.Delay(Lifetime);
        Dismiss(id);
    }
}
