using System.Windows.Input;
using QuickTranslate.Models;
using Xunit;

namespace QuickTranslate.Tests;

public class HotkeyConfigTests
{
    [Fact]
    public void Default_HasControlAndShiftAndT()
    {
        var config = HotkeyConfig.Default;

        Assert.True(config.IsControl);
        Assert.True(config.IsShift);
        Assert.False(config.IsAlt);
        Assert.False(config.IsWin);
        Assert.Equal(Key.T, config.Key);
        Assert.Equal("Ctrl + Shift + T", config.ToString());
    }

    [Fact]
    public void Modifiers_SetAndGet_UpdatesFlagsCorrectly()
    {
        var config = new HotkeyConfig
        {
            IsControl = false,
            IsShift = false,
            IsAlt = true,
            IsWin = true,
            Key = Key.F1
        };

        Assert.False(config.IsControl);
        Assert.False(config.IsShift);
        Assert.True(config.IsAlt);
        Assert.True(config.IsWin);
        Assert.Equal("Alt + Win + F1", config.ToString());
    }

    [Fact]
    public void Clone_ProducesIndependentCopy()
    {
        var original = new HotkeyConfig
        {
            IsControl = true,
            IsAlt = true,
            Key = Key.X
        };

        var clone = original.Clone();

        Assert.Equal(original.ToString(), clone.ToString());
        clone.Key = Key.Z;
        Assert.NotEqual(original.Key, clone.Key);
    }
}
