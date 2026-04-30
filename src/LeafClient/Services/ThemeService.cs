using Avalonia;
using Avalonia.Media;
using System;

namespace LeafClient.Services
{
    public static class ThemeService
    {
        public enum Theme
        {
            Dark,
            Auto
        }

        public static void SetTheme(string themeName)
        {
            ApplyTheme();
        }

        private static void ApplyTheme()
        {
            var app = Application.Current;
            if (app?.Resources == null) return;

            var primaryAccentColor = Color.Parse("#7B2CBF");
            var successColor = Color.Parse("#00C853");
            var errorColor = Color.Parse("#D32F2F");
            var errorHoverColor = Color.Parse("#E81123");
            var notificationColor = Color.Parse("#E91E63");
            var discordButtonColor = Color.Parse("#5865F2");

            app.Resources["PrimaryAccentColor"] = primaryAccentColor;
            app.Resources["PrimaryAccentBrush"] = new SolidColorBrush(primaryAccentColor);
            app.Resources["SuccessColor"] = successColor;
            app.Resources["SuccessBrush"] = new SolidColorBrush(successColor);
            app.Resources["ErrorColor"] = errorColor;
            app.Resources["ErrorBrush"] = new SolidColorBrush(errorColor);
            app.Resources["ErrorHoverColor"] = errorHoverColor;
            app.Resources["ErrorHoverBrush"] = new SolidColorBrush(errorHoverColor);
            app.Resources["NotificationColor"] = notificationColor;
            app.Resources["NotificationBrush"] = new SolidColorBrush(notificationColor);
            app.Resources["DiscordButtonColor"] = discordButtonColor;
            app.Resources["DiscordButtonBrush"] = new SolidColorBrush(discordButtonColor);

            var accentGradient = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative)
            };
            accentGradient.GradientStops.Add(new GradientStop(Color.Parse("#2DD6C1"), 0));
            accentGradient.GradientStops.Add(new GradientStop(Color.Parse("#8465FF"), 1));
            app.Resources["AccentGradientBrush"] = accentGradient;

            var launchGradient = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative)
            };
            launchGradient.GradientStops.Add(new GradientStop(Color.Parse("#00E676"), 0));
            launchGradient.GradientStops.Add(new GradientStop(Color.Parse("#00C853"), 1));
            app.Resources["LaunchGradientBrush"] = launchGradient;

            var primaryBackgroundColor = Color.Parse("#0D0D0D");
            var secondaryBackgroundColor = Color.Parse("#0A0A0A");
            var tertiaryBackgroundColor = Color.Parse("#0F0F0F");
            var cardBackgroundColor = Color.Parse("#1A1A1A");
            var hoverBackgroundColor = Color.Parse("#2A2A2A");
            var primaryBorderColor = Color.Parse("#1A1A1A");
            var secondaryBorderColor = Color.Parse("#2A2A2A");

            var primaryForegroundColor = Color.Parse("#FFFFFF");
            var secondaryForegroundColor = Color.Parse("#888888");
            var disabledForegroundColor = Color.Parse("#666666");
            var windowButtonForegroundColor = Color.Parse("#FFFFFF");
            var accentButtonForegroundColor = Color.Parse("#FFFFFF");

            var overlayColor = Color.FromArgb(0x80, 0x00, 0x00, 0x00);
            var transparentBorderColor = Color.FromArgb(0x40, 0xFF, 0xFF, 0xFF);

            app.Resources["PrimaryBackgroundColor"] = primaryBackgroundColor;
            app.Resources["SecondaryBackgroundColor"] = secondaryBackgroundColor;
            app.Resources["TertiaryBackgroundColor"] = tertiaryBackgroundColor;
            app.Resources["CardBackgroundColor"] = cardBackgroundColor;
            app.Resources["HoverBackgroundColor"] = hoverBackgroundColor;
            app.Resources["PrimaryBorderColor"] = primaryBorderColor;
            app.Resources["SecondaryBorderColor"] = secondaryBorderColor;
            app.Resources["PrimaryForegroundColor"] = primaryForegroundColor;
            app.Resources["SecondaryForegroundColor"] = secondaryForegroundColor;
            app.Resources["DisabledForegroundColor"] = disabledForegroundColor;
            app.Resources["WindowButtonForegroundColor"] = windowButtonForegroundColor;
            app.Resources["AccentButtonForegroundColor"] = accentButtonForegroundColor;
            app.Resources["OverlayColor"] = overlayColor;
            app.Resources["TransparentBorderColor"] = transparentBorderColor;

            app.Resources["PrimaryBackgroundBrush"] = new SolidColorBrush(primaryBackgroundColor);
            app.Resources["SecondaryBackgroundBrush"] = new SolidColorBrush(secondaryBackgroundColor);
            app.Resources["TertiaryBackgroundBrush"] = new SolidColorBrush(tertiaryBackgroundColor);
            app.Resources["CardBackgroundBrush"] = new SolidColorBrush(cardBackgroundColor);
            app.Resources["HoverBackgroundBrush"] = new SolidColorBrush(hoverBackgroundColor);
            app.Resources["PrimaryBorderBrush"] = new SolidColorBrush(primaryBorderColor);
            app.Resources["SecondaryBorderBrush"] = new SolidColorBrush(secondaryBorderColor);
            app.Resources["PrimaryForegroundBrush"] = new SolidColorBrush(primaryForegroundColor);
            app.Resources["SecondaryForegroundBrush"] = new SolidColorBrush(secondaryForegroundColor);
            app.Resources["DisabledForegroundBrush"] = new SolidColorBrush(disabledForegroundColor);
            app.Resources["WindowButtonForegroundBrush"] = new SolidColorBrush(windowButtonForegroundColor);
            app.Resources["AccentButtonForegroundBrush"] = new SolidColorBrush(accentButtonForegroundColor);
            app.Resources["OverlayBrush"] = new SolidColorBrush(overlayColor);
            app.Resources["TransparentBorderBrush"] = new SolidColorBrush(transparentBorderColor);

            app.Resources["LogoBackgroundBrush"] = new SolidColorBrush(cardBackgroundColor);
            app.Resources["SidebarButtonBackgroundBrush"] = new SolidColorBrush(Colors.Transparent);
        }
    }
}
