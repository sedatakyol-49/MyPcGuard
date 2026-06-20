using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using MyPcGuard.Services.Abstractions;

namespace MyPcGuard.Services.Common;

public sealed class ConfirmationDialogService(ILocalizationService localizationService) : IConfirmationDialogService
{
    public async Task<bool> ConfirmAsync(string title, string message, CancellationToken cancellationToken)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop
            || desktop.MainWindow is null)
        {
            return false;
        }

        var result = false;
        var window = new Window
        {
            Title = title,
            Width = 460,
            Height = 220,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        window.Content = BuildContent(window, message, confirmed => result = confirmed);

        await window.ShowDialog(desktop.MainWindow);
        cancellationToken.ThrowIfCancellationRequested();
        return result;
    }

    private Control BuildContent(Window window, string message, Action<bool> setResult)
    {
        var text = new TextBlock
        {
            Text = message,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 18)
        };

        var cancel = new Button { Content = localizationService.GetString("Common_Cancel"), MinWidth = 110, Padding = new Thickness(12, 8) };
        var confirm = new Button { Content = localizationService.GetString("Common_Confirm"), MinWidth = 120, Padding = new Thickness(12, 8) };
        cancel.Click += (_, args) =>
        {
            setResult(false);
            window.Close();
        };
        confirm.Click += (_, args) =>
        {
            setResult(true);
            window.Close();
        };

        return new Grid
        {
            Margin = new Thickness(22),
            RowDefinitions = new RowDefinitions("*,Auto"),
            Children =
            {
                text,
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Spacing = 10,
                    Children = { cancel, confirm },
                    [Grid.RowProperty] = 1
                }
            }
        };
    }
}
