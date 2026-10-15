using System.Windows.Media.Imaging;
using NSubstitute;
using QuickTranslate.Infrastructure;
using Xunit;

namespace QuickTranslate.Tests;

public class AppIconTests
{
    [Fact]
    public void CreateBitmap_GeneratesValidBitmapWithExpectedSize()
    {
        using var bitmap = AppIcon.CreateBitmap(64);

        Assert.NotNull(bitmap);
        Assert.Equal(64, bitmap.Width);
        Assert.Equal(64, bitmap.Height);

        // Center pixel should not be transparent (letter Q or background)
        var centerPixel = bitmap.GetPixel(32, 32);
        Assert.True(centerPixel.A > 0);
    }

    [Fact]
    public void CreateTrayIcon_GeneratesValidIcon()
    {
        using var icon = AppIcon.CreateTrayIcon();

        Assert.NotNull(icon);
        Assert.NotEqual(IntPtr.Zero, icon.Handle);
        Assert.Equal(64, icon.Width);
        Assert.Equal(64, icon.Height);
    }

    [Fact]
    public void GetImageSource_ReturnsFrozenImageSourceWithExpectedDimensions()
    {
        var imageSource = AppIcon.GetImageSource();

        Assert.NotNull(imageSource);
        Assert.True(imageSource.IsFrozen);

        var bitmapSource = Assert.IsAssignableFrom<BitmapSource>(imageSource);
        Assert.Equal(64, bitmapSource.PixelWidth);
        Assert.Equal(64, bitmapSource.PixelHeight);

        // Same cached instance returned
        var secondCall = AppIcon.GetImageSource();
        Assert.Same(imageSource, secondCall);
    }

    [Fact]
    public void CreateIcoData_GeneratesValidIcoBytes()
    {
        var icoBytes = AppIcon.CreateIcoData(16, 32, 64);

        Assert.NotNull(icoBytes);
        Assert.True(icoBytes.Length > 0);

        // Header checks: reserved 0, type 1, count 3
        Assert.Equal(0, BitConverter.ToUInt16(icoBytes, 0));
        Assert.Equal(1, BitConverter.ToUInt16(icoBytes, 2));
        Assert.Equal(3, BitConverter.ToUInt16(icoBytes, 4));
    }

    [Fact]
    public void SettingsWindow_HasTrayIconSet()
    {
        Exception? threadEx = null;
        var thread = new Thread(() =>
        {
            try
            {
                var settingsService = NSubstitute.Substitute.For<QuickTranslate.Services.ISettingsService>();
                settingsService.CurrentSettings.Returns(new QuickTranslate.Models.AppSettings
                {
                    Hotkey = new QuickTranslate.Models.HotkeyConfig { IsControl = true, IsShift = true, Key = System.Windows.Input.Key.T },
                    SourceLanguage = "auto",
                    TargetLanguage = "ru",
                    StartWithWindows = false
                });

                var vm = new QuickTranslate.ViewModels.SettingsViewModel(
                    settingsService,
                    NSubstitute.Substitute.For<QuickTranslate.Services.IHotkeyService>(),
                    NSubstitute.Substitute.For<QuickTranslate.Services.IStartupService>(),
                    Microsoft.Extensions.Logging.Abstractions.NullLogger<QuickTranslate.ViewModels.SettingsViewModel>.Instance);

                var window = new QuickTranslate.Views.SettingsWindow(vm);
                Assert.NotNull(window.Icon);
                Assert.Same(AppIcon.ImageSource, window.Icon);
            }
            catch (Exception ex)
            {
                threadEx = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (threadEx != null)
        {
            throw new Exception($"Test thread failed: {threadEx}");
        }
    }
}
