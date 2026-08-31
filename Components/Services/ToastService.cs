namespace TruvoID.Components.Services;

public enum ToastType { Success, Error, Info, Warning }

public record Toast(string Message, ToastType Type, string Id = "");

public class ToastService
{
    public event Action<Toast>? OnShow;

    public void Show(string message, ToastType type = ToastType.Info)
        => OnShow?.Invoke(new Toast(message, type, Guid.NewGuid().ToString("N")[..8]));

    public void Success(string message) => Show(message, ToastType.Success);
    public void Error(string message) => Show(message, ToastType.Error);
    public void Warning(string message) => Show(message, ToastType.Warning);
    public void Info(string message) => Show(message, ToastType.Info);
}
