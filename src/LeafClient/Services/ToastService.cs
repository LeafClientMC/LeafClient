using System;

namespace LeafClient.Services
{
    public enum ToastType { Info, Success, Error }

    /// <summary>
    /// Lightweight static event bus. Call <see cref="Show"/> from anywhere;
    /// MainWindow subscribes and renders the toasts.
    /// </summary>
    public static class ToastService
    {
        public static event Action<string, ToastType>? ToastRequested;

        public static void Show(string message, ToastType type = ToastType.Info)
            => ToastRequested?.Invoke(message, type);
    }
}
