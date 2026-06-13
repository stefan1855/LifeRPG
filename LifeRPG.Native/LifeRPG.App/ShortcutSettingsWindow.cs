using SW = System.Windows;
using SWC = System.Windows.Controls;
using SWI = System.Windows.Input;
using SWM = System.Windows.Media;
using MessageBox = System.Windows.MessageBox;
using LifeRPG.Storage.Services;

namespace LifeRPG.App;

public class ShortcutSettingsWindow : SW.Window
{
    private readonly Dictionary<string, string> _shortcuts;
    private string? _editingName;

    public ShortcutSettingsWindow()
    {
        Title = "快捷键设置";
        Width = 500; Height = 380;
        ResizeMode = SW.ResizeMode.NoResize;
        WindowStartupLocation = SW.WindowStartupLocation.CenterOwner;
        WindowStyle = SW.WindowStyle.ToolWindow;
        Background = Brush("#181825");

        KeyDown += OnKeyDown;

        _shortcuts = AppConfig.GetAllShortcuts().ToDictionary(kv => kv.Key, kv => kv.Value);
        BuildUi();
    }

    private static SWM.Color ParseColor(string hex) =>
        (SWM.Color)SWM.ColorConverter.ConvertFromString(hex);

    private static SWM.SolidColorBrush Brush(string hex) => new(ParseColor(hex));

    private readonly List<(SWC.Border border, SWC.TextBlock label)> _slots = [];

    private void BuildUi()
    {
        var root = new SWC.Grid { Margin = new SW.Thickness(20) };
        root.RowDefinitions.Add(new SWC.RowDefinition { Height = SW.GridLength.Auto });
        root.RowDefinitions.Add(new SWC.RowDefinition { Height = new SW.GridLength(1, SW.GridUnitType.Star) });
        root.RowDefinitions.Add(new SWC.RowDefinition { Height = SW.GridLength.Auto });

        // Title
        var titleStack = new SWC.StackPanel();
        titleStack.Children.Add(new SWC.TextBlock
        {
            Text = "快捷键设置", FontSize = 18, FontWeight = SW.FontWeights.Bold,
            Foreground = Brush("#CDD6F4"), Margin = new SW.Thickness(0, 0, 0, 4)
        });
        titleStack.Children.Add(new SWC.TextBlock
        {
            Text = "点击右侧方框（变绿边框）→ 按下组合键",
            FontSize = 13, Foreground = Brush("#A6ADC8"), Margin = new SW.Thickness(0, 0, 0, 16)
        });
        SWC.Grid.SetRow(titleStack, 0);
        root.Children.Add(titleStack);

        // Rows
        var rowsPanel = new SWC.StackPanel();
        SWC.Grid.SetRow(rowsPanel, 1);
        root.Children.Add(rowsPanel);

        foreach (var (name, value) in _shortcuts)
        {
            var row = new SWC.Grid { Margin = new SW.Thickness(0, 0, 0, 12) };
            row.ColumnDefinitions.Add(new SWC.ColumnDefinition { Width = new SW.GridLength(100) });
            row.ColumnDefinitions.Add(new SWC.ColumnDefinition { Width = new SW.GridLength(10) });
            row.ColumnDefinitions.Add(new SWC.ColumnDefinition { Width = SW.GridLength.Auto });

            row.Children.Add(new SWC.TextBlock
            {
                Text = name, Foreground = Brush("#CDD6F4"),
                FontSize = 14, VerticalAlignment = SW.VerticalAlignment.Center
            });

            var valueLabel = new SWC.TextBlock
            {
                Text = value, Foreground = Brush("#A6E3A1"),
                FontSize = 14, FontWeight = SW.FontWeights.Bold,
                HorizontalAlignment = SW.HorizontalAlignment.Center,
                VerticalAlignment = SW.VerticalAlignment.Center
            };

            var border = new SWC.Border
            {
                Width = 190, Height = 32,
                Background = Brush("#1E1E2E"),
                BorderBrush = Brush("#45475A"),
                BorderThickness = new SW.Thickness(1),
                CornerRadius = new SW.CornerRadius(4),
                Cursor = SWI.Cursors.Hand,
                Child = valueLabel,
                Tag = name
            };
            border.MouseLeftButtonDown += (_, _) =>
            {
                _editingName = name;
                foreach (var (b, _) in _slots)
                {
                    var isActive = b.Tag as string == _editingName;
                    b.BorderBrush = Brush(isActive ? "#A6E3A1" : "#45475A");
                    b.Background = Brush(isActive ? "#2A2A3E" : "#1E1E2E");
                }
            };

            SWC.Grid.SetColumn(border, 2);
            row.Children.Add(border);
            rowsPanel.Children.Add(row);
            _slots.Add((border, valueLabel));
        }

        // Buttons
        var buttonRow = new SWC.StackPanel
        {
            Orientation = SWC.Orientation.Horizontal,
            HorizontalAlignment = SW.HorizontalAlignment.Right,
            Margin = new SW.Thickness(0, 16, 0, 0)
        };
        SWC.Grid.SetRow(buttonRow, 2);

        var resetBtn = new SWC.Button
        {
            Content = "默认", Width = 80, Height = 32,
            Background = Brush("#313244"), Foreground = Brush("#CDD6F4"),
            BorderBrush = Brush("#45475A"), BorderThickness = new SW.Thickness(1),
            FontSize = 13, Cursor = SWI.Cursors.Hand
        };
        resetBtn.Click += (_, _) =>
        {
            _shortcuts["新建记录"] = "Ctrl+N";
            _shortcuts["保存"] = "Ctrl+S";
            _shortcuts["搜索"] = "Ctrl+F";
            BuildUi();
            _editingName = null;
        };

        var saveBtn = new SWC.Button
        {
            Content = "保存", Width = 80, Height = 32,
            Background = Brush("#3A2552"), Foreground = Brush("#A6E3A1"),
            BorderBrush = Brush("#A6E3A1"), BorderThickness = new SW.Thickness(1),
            FontSize = 13, FontWeight = SW.FontWeights.Bold, Cursor = SWI.Cursors.Hand,
            Margin = new SW.Thickness(10, 0, 0, 0)
        };
        saveBtn.Click += (_, _) =>
        {
            AppConfig.SaveShortcuts(_shortcuts);
            MessageBox.Show("快捷键已保存。", "LifeRPG.Native",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            Close();
        };

        buttonRow.Children.Add(resetBtn);
        buttonRow.Children.Add(saveBtn);
        root.Children.Add(buttonRow);

        Content = root;
    }

    private void OnKeyDown(object sender, SWI.KeyEventArgs e)
    {
        if (string.IsNullOrEmpty(_editingName)) return;
        var key = e.Key == SWI.Key.System ? e.SystemKey : e.Key;
        if (key is SWI.Key.LeftCtrl or SWI.Key.RightCtrl or SWI.Key.LeftAlt or SWI.Key.RightAlt
            or SWI.Key.LeftShift or SWI.Key.RightShift or SWI.Key.LWin or SWI.Key.RWin
            or SWI.Key.Tab or SWI.Key.Escape)
            return;

        var mods = SWI.Keyboard.Modifiers;
        if (mods == SWI.ModifierKeys.None) return;

        var display = KeyToString(key, mods);
        _shortcuts[_editingName] = display;

        foreach (var (b, label) in _slots)
        {
            if (b.Tag as string == _editingName)
                label.Text = display;
        }
        e.Handled = true;
    }

    private static string KeyToString(SWI.Key key, SWI.ModifierKeys mods)
    {
        var parts = new List<string>();
        if ((mods & SWI.ModifierKeys.Control) != 0) parts.Add("Ctrl");
        if ((mods & SWI.ModifierKeys.Alt) != 0) parts.Add("Alt");
        if ((mods & SWI.ModifierKeys.Shift) != 0) parts.Add("Shift");
        parts.Add(key.ToString());
        return string.Join("+", parts);
    }
}
