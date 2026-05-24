using System;

namespace LeafClient.Services
{
    public enum ToastType { Info, Success, Error }

    public static class ToastService
    {
        public static event Action<string, ToastType>? ToastRequested;

        public static void Show(string message, ToastType type = ToastType.Info)
            => ToastRequested?.Invoke(message, type);
    }
}
