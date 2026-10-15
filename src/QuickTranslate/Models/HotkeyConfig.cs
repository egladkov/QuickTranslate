using System.Text.Json.Serialization;
using System.Windows.Input;

namespace QuickTranslate.Models;

[Flags]
public enum HotkeyModifiers
{
    None = 0,
    Alt = 1,
    Control = 2,
    Shift = 4,
    Win = 8
}

public class HotkeyConfig
{
    public HotkeyModifiers Modifiers { get; set; } = HotkeyModifiers.Control | HotkeyModifiers.Shift;
    public Key Key { get; set; } = Key.T;

    [JsonIgnore]
    public bool IsControl
    {
        get => Modifiers.HasFlag(HotkeyModifiers.Control);
        set => SetModifier(HotkeyModifiers.Control, value);
    }

    [JsonIgnore]
    public bool IsShift
    {
        get => Modifiers.HasFlag(HotkeyModifiers.Shift);
        set => SetModifier(HotkeyModifiers.Shift, value);
    }

    [JsonIgnore]
    public bool IsAlt
    {
        get => Modifiers.HasFlag(HotkeyModifiers.Alt);
        set => SetModifier(HotkeyModifiers.Alt, value);
    }

    [JsonIgnore]
    public bool IsWin
    {
        get => Modifiers.HasFlag(HotkeyModifiers.Win);
        set => SetModifier(HotkeyModifiers.Win, value);
    }

    private void SetModifier(HotkeyModifiers modifier, bool set)
    {
        if (set)
            Modifiers |= modifier;
        else
            Modifiers &= ~modifier;
    }

    public override string ToString()
    {
        var parts = new List<string>();
        if (IsControl) parts.Add("Ctrl");
        if (IsShift) parts.Add("Shift");
        if (IsAlt) parts.Add("Alt");
        if (IsWin) parts.Add("Win");
        parts.Add(Key.ToString());
        return string.Join(" + ", parts);
    }

    public static HotkeyConfig Default => new()
    {
        Modifiers = HotkeyModifiers.Control | HotkeyModifiers.Shift,
        Key = Key.T
    };

    public HotkeyConfig Clone()
    {
        return new HotkeyConfig
        {
            Modifiers = Modifiers,
            Key = Key
        };
    }
}
