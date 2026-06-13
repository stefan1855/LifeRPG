using System.Windows;
using System.Windows.Input;
using System.Linq;
using LifeRPG.Storage.Services;
using MessageBox = System.Windows.MessageBox;

namespace LifeRPG.App;

public partial class ShortcutSettingsWindow : Window
{
    private readonly Dictionary<string, string> _shortcuts;

    public ShortcutSettingsWindow()
    {
        InitializeComponent();
        _shortcuts = AppConfig.GetAllShortcuts().ToDictionary(kv => kv.Key, kv => kv.Value);
        RefreshList();
    }

    private void RefreshList()
    {
        ShortcutsList.ItemsSource = _shortcuts.Select(kv => new ShortcutRow
        {
            Name = kv.Key,
            DisplayText = kv.Value
        }).ToList();
    }

    private void KeyCaptureBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (sender is not TextBox box) return;
        if (box.DataContext is not ShortcutRow row) return;

        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt
            or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin)
            return;

        var mods = Keyboard.Modifiers;
        if (mods == ModifierKeys.None) return;

        var display = KeyToString(key, mods);
        row.DisplayText = display;
        _shortcuts[row.Name] = display;
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
        RefreshList();
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

    private sealed class ShortcutRow
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayText { get; set; } = string.Empty;
    }
}
