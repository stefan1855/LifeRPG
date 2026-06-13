using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using LifeRPG.Storage.Services;
using MessageBox = System.Windows.MessageBox;

namespace LifeRPG.App;

public partial class ShortcutSettingsWindow : Window
{
    private readonly Dictionary<string, string> _shortcuts;
    private string? _editingName;

    private static readonly System.Windows.Media.Brush SlotBg = new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x2E));
    private static readonly System.Windows.Media.Brush SlotBorder = new SolidColorBrush(Color.FromRgb(0x45, 0x47, 0x5A));
    private static readonly System.Windows.Media.Brush SlotActiveBorder = new SolidColorBrush(Color.FromRgb(0xA6, 0xE3, 0xA1));
    private static readonly System.Windows.Media.Brush SlotActiveBg = new SolidColorBrush(Color.FromRgb(0x2A, 0x2A, 0x3E));
    private static readonly System.Windows.Media.Brush TextFg = new SolidColorBrush(Color.FromRgb(0xCD, 0xD6, 0xF4));
    private static readonly System.Windows.Media.Brush ActiveFg = new SolidColorBrush(Color.FromRgb(0xA6, 0xE3, 0xA1));

    public ShortcutSettingsWindow()
    {
        InitializeComponent();
        _shortcuts = AppConfig.GetAllShortcuts().ToDictionary(kv => kv.Key, kv => kv.Value);
        BuildRows();
    }

    private readonly List<(Border border, TextBlock label)> _slots = [];

    private void BuildRows()
    {
        RowsPanel.Children.Clear();
        _slots.Clear();
        foreach (var (name, value) in _shortcuts)
        {
            var row = new Grid { Margin = new Thickness(0, 0, 0, 12) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var label = new TextBlock
            {
                Text = name,
                Foreground = TextFg,
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(label, 0);
            row.Children.Add(label);

            var valueLabel = new TextBlock
            {
                Text = value,
                Foreground = ActiveFg,
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var border = new Border
            {
                Width = 190, Height = 32,
                Background = SlotBg,
                BorderBrush = SlotBorder,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Cursor = Cursors.Hand,
                Child = valueLabel,
                Tag = name
            };
            border.MouseLeftButtonDown += (_, _) =>
            {
                _editingName = name as string;
                foreach (var (b, _) in _slots)
                {
                    b.BorderBrush = b.Tag as string == _editingName ? SlotActiveBorder : SlotBorder;
                    b.Background = b.Tag as string == _editingName ? SlotActiveBg : SlotBg;
                }
            };

            Grid.SetColumn(border, 2);
            row.Children.Add(border);
            RowsPanel.Children.Add(row);
            _slots.Add((border, valueLabel));
        }
    }

    private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (string.IsNullOrEmpty(_editingName)) return;
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt
            or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin or Key.Tab
            or Key.Escape)
            return;

        var mods = Keyboard.Modifiers;
        if (mods == ModifierKeys.None) return;

        var display = KeyToString(key, mods);
        _shortcuts[_editingName] = display;

        foreach (var (b, label) in _slots)
        {
            if (b.Tag as string == _editingName)
                label.Text = display;
        }
        e.Handled = true;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        AppConfig.SaveShortcuts(_shortcuts);
        MessageBox.Show("快捷键已保存。", "LifeRPG.Native", MessageBoxButton.OK, MessageBoxImage.Information);
        Close();
    }

    private void ResetDefaultsButton_Click(object sender, RoutedEventArgs e)
    {
        _shortcuts["新建记录"] = "Ctrl+N";
        _shortcuts["保存"] = "Ctrl+S";
        _shortcuts["搜索"] = "Ctrl+F";
        BuildRows();
        _editingName = null;
    }

    private static string KeyToString(Key key, ModifierKeys mods)
    {
        var parts = new List<string>();
        if ((mods & ModifierKeys.Control) != 0) parts.Add("Ctrl");
        if ((mods & ModifierKeys.Alt) != 0) parts.Add("Alt");
        if ((mods & ModifierKeys.Shift) != 0) parts.Add("Shift");
        parts.Add(key.ToString());
        return string.Join("+", parts);
    }
}
