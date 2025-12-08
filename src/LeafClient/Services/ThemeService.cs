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
            Light,
            Auto
        }

        public static void SetTheme(string themeName)
        {
            Theme theme;
            if (Enum.TryParse<Theme>(themeName, true, out var parsedTheme))
            {
                theme = parsedTheme;
            }
            else
            {
                theme = Theme.Dark; // Default to Dark if parsing fails
            }

            if (theme == Theme.Auto)
            {
                theme = Theme.Dark; // Default to Dark if auto is selected for now
            }

            ApplyTheme(theme);
        }

        private static void ApplyTheme(Theme theme)
        {
            var app = Application.Current;
            if (app?.Resources == null) return;

            // Define common accent colors that don't change with theme
            var primaryAccentColor = Color.Parse("#7B2CBF");
            var successColor = Color.Parse("#00C853");
            var errorColor = Color.Parse("#D32F2F");
            var errorHoverColor = Color.Parse("#E81123"); // Specific hover for close button
            var notificationColor = Color.Parse("#E91E63"); // For "NEW" tags
            var discordButtonColor = Color.Parse("#5865F2"); // Specific Discord button color

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


            // Accent Gradient Brush (unchanged by theme)
            var accentGradient = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative)
            };
            accentGradient.GradientStops.Add(new GradientStop(Color.Parse("#2DD6C1"), 0));
            accentGradient.GradientStops.Add(new GradientStop(Color.Parse("#8465FF"), 1));
            app.Resources["AccentGradientBrush"] = accentGradient;

            // Launch Gradient Brush (unchanged by theme)
            var launchGradient = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative)
            };
            launchGradient.GradientStops.Add(new GradientStop(Color.Parse("#00E676"), 0));
            launchGradient.GradientStops.Add(new GradientStop(Color.Parse("#00C853"), 1));
            app.Resources["LaunchGradientBrush"] = launchGradient;


            if (theme == Theme.Dark)
            {
                // Dark Theme Base Colors
                var primaryBackgroundColor = Color.Parse("#0D0D0D");
                var secondaryBackgroundColor = Color.Parse("#0A0A0A");
                var tertiaryBackgroundColor = Color.Parse("#0F0F0F");
                var cardBackgroundColor = Color.Parse("#1A1A1A");
                var hoverBackgroundColor = Color.Parse("#2A2A2A"); // Dark hover for dark theme
                var primaryBorderColor = Color.Parse("#1A1A1A");
                var secondaryBorderColor = Color.Parse("#2A2A2A");

                var primaryForegroundColor = Color.Parse("#FFFFFF");
                var secondaryForegroundColor = Color.Parse("#888888");
                var disabledForegroundColor = Color.Parse("#666666");
                var windowButtonForegroundColor = Color.Parse("#FFFFFF"); // White text for window controls
                var accentButtonForegroundColor = Color.Parse("#FFFFFF"); // White text on accent buttons

                var overlayColor = Color.FromArgb(0x80, 0x00, 0x00, 0x00); // Semi-transparent black
                var transparentBorderColor = Color.FromArgb(0x40, 0xFF, 0xFF, 0xFF); // Semi-transparent white border

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

                // Update SolidColorBrushes for Dark Theme
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

                // Sidebar/Logo Button Backgrounds for Dark Theme (transparent or matching card background)
                app.Resources["LogoBackgroundBrush"] = new SolidColorBrush(cardBackgroundColor); // Or transparent if preferred
                app.Resources["SidebarButtonBackgroundBrush"] = new SolidColorBrush(Colors.Transparent);
            }
            else // Light Theme
            {
                // Light Theme Base Colors
                var primaryBackgroundColor = Color.Parse("#F5F5F7");
                var secondaryBackgroundColor = Color.Parse("#FFFFFF");
                var tertiaryBackgroundColor = Color.Parse("#FAFAFA");
                var cardBackgroundColor = Color.Parse("#EBEBEB");
                var hoverBackgroundColor = Color.Parse("#D0D0D0"); // Darker gray hover for light theme
                var primaryBorderColor = Color.Parse("#D0D0D0");
                var secondaryBorderColor = Color.Parse("#C0C0C0");

                var primaryForegroundColor = Color.Parse("#1C1C1E"); // Dark text
                var secondaryForegroundColor = Color.Parse("#6E6E73"); // Darker gray text
                var disabledForegroundColor = Color.Parse("#AEAEB2"); // Light gray disabled text
                var windowButtonForegroundColor = Color.Parse("#1C1C1E"); // Dark text for window controls
                var accentButtonForegroundColor = Color.Parse("#FFFFFF"); // White text on accent buttons (for contrast)

                var overlayColor = Color.FromArgb(0x80, 0xFF, 0xFF, 0xFF); // Semi-transparent white
                var transparentBorderColor = Color.FromArgb(0x40, 0x00, 0x00, 0x00); // Semi-transparent black border

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

                // Update SolidColorBrushes for Light Theme
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

                // Sidebar/Logo Button Backgrounds for Light Theme (dark gray)
                app.Resources["LogoBackgroundBrush"] = new SolidColorBrush(Color.Parse("#1A1A1A")); // Dark gray for contrast
                app.Resources["SidebarButtonBackgroundBrush"] = new SolidColorBrush(Color.Parse("#1A1A1A")); // Dark gray for contrast
            }
        }
    }
}
