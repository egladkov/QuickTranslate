using System.Threading;
using System.Windows;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using QuickTranslate.Models;
using QuickTranslate.Services;
using QuickTranslate.ViewModels;
using QuickTranslate.Views;
using Xunit;

namespace QuickTranslate.Tests;

public class TranslationWindowLayoutTests
{
    [Fact]
    public void TranslationWindow_HasConstantWidthConfiguration()
    {
        var thread = new Thread(() =>
        {
            var settingsService = Substitute.For<ISettingsService>();
            settingsService.CurrentSettings.Returns(new AppSettings());
            var transService = Substitute.For<ITranslationService>();
            var clipService = Substitute.For<IClipboardService>();
            var vm = new TranslationViewModel(
                transService,
                clipService,
                settingsService,
                NullLogger<TranslationViewModel>.Instance);

            var win = new TranslationWindow(vm, settingsService);

            Assert.Equal(440, win.Width);
            Assert.Equal(440, win.MinWidth);
            Assert.Equal(440, win.MaxWidth);
            Assert.Equal(ResizeMode.NoResize, win.ResizeMode);
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
    }

    [Fact]
    public void TranslationWindow_WidthRemainsConstantDuringAndAfterTranslation()
    {
        var thread = new Thread(() =>
        {
            var app = System.Windows.Application.Current ?? new System.Windows.Application();
            if (!app.Resources.MergedDictionaries.Any())
            {
                app.Resources.MergedDictionaries.Add(new ResourceDictionary
                {
                    Source = new Uri("pack://application:,,,/QuickTranslate;component/Themes/LightTheme.xaml", UriKind.Absolute)
                });
            }

            var settingsService = Substitute.For<ISettingsService>();
            settingsService.CurrentSettings.Returns(new AppSettings());
            var transService = Substitute.For<ITranslationService>();
            transService.TranslateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(new TranslationResult
                {
                    IsSuccess = true,
                    TranslatedText = "Тестовый результат перевода текста.",
                    DetectedSourceLanguage = "en"
                }));
            var clipService = Substitute.For<IClipboardService>();
            var vm = new TranslationViewModel(
                transService,
                clipService,
                settingsService,
                NullLogger<TranslationViewModel>.Instance);

            var win = new TranslationWindow(vm, settingsService);

            win.ShowAtOptimalPosition();
            System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Loaded);

            double widthInitial = win.ActualWidth > 0 ? win.ActualWidth : win.Width;

            // Start translation (loading state)
            var translationTask = vm.StartTranslationAsync("Short text");
            System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Loaded);

            double widthDuringTranslation = win.ActualWidth > 0 ? win.ActualWidth : win.Width;

            // Wait for completion (loaded state)
            translationTask.Wait();
            System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Loaded);

            double widthAfterTranslation = win.ActualWidth > 0 ? win.ActualWidth : win.Width;

            win.AllowActualClose = true;
            win.Close();

            Assert.Equal(440, widthInitial);
            Assert.Equal(440, widthDuringTranslation);
            Assert.Equal(440, widthAfterTranslation);
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
    }

    [Fact]
    public void TranslationWindow_SelectedTextMode_SetsReadOnlyAndHidesTranslateButton()
    {
        var thread = new Thread(() =>
        {
            var app = System.Windows.Application.Current ?? new System.Windows.Application();
            if (!app.Resources.MergedDictionaries.Any())
            {
                app.Resources.MergedDictionaries.Add(new ResourceDictionary
                {
                    Source = new Uri("pack://application:,,,/QuickTranslate;component/Themes/LightTheme.xaml", UriKind.Absolute)
                });
            }

            var settingsService = Substitute.For<ISettingsService>();
            settingsService.CurrentSettings.Returns(new AppSettings());
            var transService = Substitute.For<ITranslationService>();
            transService.TranslateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(new TranslationResult
                {
                    IsSuccess = true,
                    TranslatedText = "Привет",
                    DetectedSourceLanguage = "en"
                });
            var clipService = Substitute.For<IClipboardService>();
            var vm = new TranslationViewModel(
                transService,
                clipService,
                settingsService,
                NullLogger<TranslationViewModel>.Instance);

            var win = new TranslationWindow(vm, settingsService);
            win.Show();
            System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Loaded);

            var inputTextBox = (System.Windows.Controls.TextBox)win.FindName("InputTextBox");

            // Initially or in manual mode
            vm.PrepareForManualInput();
            System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Loaded);
            Assert.False(inputTextBox.IsReadOnly);

            // In selected text mode
            var task = vm.StartTranslationAsync("Hello", isFromSelection: true);
            task.Wait();
            System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Loaded);

            Assert.True(inputTextBox.IsReadOnly);
            Assert.True(vm.IsSourceReadOnly);
            Assert.False(vm.IsTranslateButtonVisible);

            win.AllowActualClose = true;
            win.Close();
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
    }

    [Fact]
    public void TranslationWindow_ClickOnTranslationResult_CopiesTranslationToClipboard()
    {
        var thread = new Thread(() =>
        {
            SynchronizationContext.SetSynchronizationContext(new System.Windows.Threading.DispatcherSynchronizationContext());

            var app = System.Windows.Application.Current ?? new System.Windows.Application();
            if (!app.Resources.MergedDictionaries.Any())
            {
                app.Resources.MergedDictionaries.Add(new ResourceDictionary
                {
                    Source = new Uri("pack://application:,,,/QuickTranslate;component/Themes/LightTheme.xaml", UriKind.Absolute)
                });
            }

            var settingsService = Substitute.For<ISettingsService>();
            settingsService.CurrentSettings.Returns(new AppSettings());
            var transService = Substitute.For<ITranslationService>();
            var clipService = Substitute.For<IClipboardService>();
            var vm = new TranslationViewModel(
                transService,
                clipService,
                settingsService,
                NullLogger<TranslationViewModel>.Instance);

            vm.TranslatedText = "Скопированный текст перевода";

            var win = new TranslationWindow(vm, settingsService);
            win.Show();
            System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Loaded);

            var container = (System.Windows.Controls.Border)win.FindName("TranslationResultContainer");
            Assert.NotNull(container);

            container.RaiseEvent(new System.Windows.Input.MouseButtonEventArgs(
                System.Windows.Input.Mouse.PrimaryDevice,
                0,
                System.Windows.Input.MouseButton.Left)
            {
                RoutedEvent = UIElement.MouseLeftButtonUpEvent
            });

            clipService.Received(1).SetTextAsync("Скопированный текст перевода");
            Assert.True(vm.IsCopied);

            vm.ResetCopiedState();
            System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Loaded);

            win.DataContext = null;
            win.AllowActualClose = true;
            win.Close();
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
    }

    [Fact]
    public void TranslationWindow_ClickOnTranslationResult_WhenEmpty_DoesNotCopy()
    {
        var thread = new Thread(() =>
        {
            SynchronizationContext.SetSynchronizationContext(new System.Windows.Threading.DispatcherSynchronizationContext());

            var app = System.Windows.Application.Current ?? new System.Windows.Application();
            if (!app.Resources.MergedDictionaries.Any())
            {
                app.Resources.MergedDictionaries.Add(new ResourceDictionary
                {
                    Source = new Uri("pack://application:,,,/QuickTranslate;component/Themes/LightTheme.xaml", UriKind.Absolute)
                });
            }

            var settingsService = Substitute.For<ISettingsService>();
            settingsService.CurrentSettings.Returns(new AppSettings());
            var transService = Substitute.For<ITranslationService>();
            var clipService = Substitute.For<IClipboardService>();
            var vm = new TranslationViewModel(
                transService,
                clipService,
                settingsService,
                NullLogger<TranslationViewModel>.Instance);

            vm.TranslatedText = string.Empty;

            var win = new TranslationWindow(vm, settingsService);
            win.Show();
            System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Loaded);

            var container = (System.Windows.Controls.Border)win.FindName("TranslationResultContainer");
            Assert.NotNull(container);

            container.RaiseEvent(new System.Windows.Input.MouseButtonEventArgs(
                System.Windows.Input.Mouse.PrimaryDevice,
                0,
                System.Windows.Input.MouseButton.Left)
            {
                RoutedEvent = UIElement.MouseLeftButtonUpEvent
            });

            clipService.DidNotReceiveWithAnyArgs().SetTextAsync(Arg.Any<string>());
            Assert.False(vm.IsCopied);

            win.AllowActualClose = true;
            win.Close();
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
    }

    [Fact]
    public void TranslationWindow_AutoClose_WhenConfigured_InitializesTimerAndRemainingSeconds()
    {
        Exception? threadEx = null;
        var thread = new Thread(() =>
        {
            try
            {
                var settingsService = Substitute.For<ISettingsService>();
                settingsService.CurrentSettings.Returns(new AppSettings
                {
                    AutoCloseSeconds = 10
                });

                var transService = Substitute.For<ITranslationService>();
                var clipService = Substitute.For<IClipboardService>();
                var vm = new TranslationViewModel(
                    transService,
                    clipService,
                    settingsService,
                    NullLogger<TranslationViewModel>.Instance);

                vm.IsSourceReadOnly = true;

                var win = new TranslationWindow(vm, settingsService);
                win.Show();
                System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Loaded);

                win.ResetAndStartAutoCloseTimer();
                Assert.Equal(10, win.RemainingAutoCloseSeconds);
                Assert.True(win.IsAutoCloseTimerRunning);
                Assert.False(win.IsAutoClosePausedByHover);

                win.PauseAutoCloseTimer();
                Assert.False(win.IsAutoCloseTimerRunning);
                Assert.True(win.IsAutoClosePausedByHover);

                win.ResumeAutoCloseTimer();
                Assert.True(win.IsAutoCloseTimerRunning);
                Assert.False(win.IsAutoClosePausedByHover);

                win.StopAutoCloseTimer();
                Assert.False(win.IsAutoCloseTimerRunning);
                Assert.Equal(0, win.RemainingAutoCloseSeconds);

                win.AllowActualClose = true;
                win.Close();
            }
            catch (Exception ex)
            {
                threadEx = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (threadEx != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(threadEx).Throw();
    }

    [Fact]
    public void TranslationWindow_AutoClose_WhenManualInput_DoesNotStartTimer()
    {
        Exception? threadEx = null;
        var thread = new Thread(() =>
        {
            try
            {
                var settingsService = Substitute.For<ISettingsService>();
                settingsService.CurrentSettings.Returns(new AppSettings
                {
                    AutoCloseSeconds = 10
                });

                var transService = Substitute.For<ITranslationService>();
                var clipService = Substitute.For<IClipboardService>();
                var vm = new TranslationViewModel(
                    transService,
                    clipService,
                    settingsService,
                    NullLogger<TranslationViewModel>.Instance);

                vm.IsSourceReadOnly = false;

                var win = new TranslationWindow(vm, settingsService);
                win.Show();
                System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Loaded);

                win.ResetAndStartAutoCloseTimer();
                Assert.Equal(0, win.RemainingAutoCloseSeconds);
                Assert.False(win.IsAutoCloseTimerRunning);

                win.AllowActualClose = true;
                win.Close();
            }
            catch (Exception ex)
            {
                threadEx = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (threadEx != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(threadEx).Throw();
    }

    [Fact]
    public void TranslationWindow_AutoClose_WhenDisabled_DoesNotStartTimer()
    {
        Exception? threadEx = null;
        var thread = new Thread(() =>
        {
            try
            {
                var settingsService = Substitute.For<ISettingsService>();
                settingsService.CurrentSettings.Returns(new AppSettings
                {
                    AutoCloseSeconds = 0
                });

                var transService = Substitute.For<ITranslationService>();
                var clipService = Substitute.For<IClipboardService>();
                var vm = new TranslationViewModel(
                    transService,
                    clipService,
                    settingsService,
                    NullLogger<TranslationViewModel>.Instance);

                var win = new TranslationWindow(vm, settingsService);

                win.ResetAndStartAutoCloseTimer();
                Assert.Equal(0, win.RemainingAutoCloseSeconds);
                Assert.False(win.IsAutoCloseTimerRunning);

                win.AllowActualClose = true;
                win.Close();
            }
            catch (Exception ex)
            {
                threadEx = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (threadEx != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(threadEx).Throw();
    }

    [Fact]
    public void TranslationWindow_Deactivated_WhenCloseOnClickOutsideEnabled_HidesWindow()
    {
        Exception? threadEx = null;
        var thread = new Thread(() =>
        {
            try
            {
                var app = System.Windows.Application.Current ?? new System.Windows.Application();
                if (!app.Resources.MergedDictionaries.Any())
                {
                    app.Resources.MergedDictionaries.Add(new ResourceDictionary
                    {
                        Source = new Uri("pack://application:,,,/QuickTranslate;component/Themes/LightTheme.xaml", UriKind.Absolute)
                    });
                }

                var settingsService = Substitute.For<ISettingsService>();
                settingsService.CurrentSettings.Returns(new AppSettings
                {
                    CloseOnClickOutside = true
                });

                var transService = Substitute.For<ITranslationService>();
                var clipService = Substitute.For<IClipboardService>();
                var vm = new TranslationViewModel(
                    transService,
                    clipService,
                    settingsService,
                    NullLogger<TranslationViewModel>.Instance);

                var win = new TranslationWindow(vm, settingsService);
                win.Show();
                System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Loaded);

                Assert.Equal(Visibility.Visible, win.Visibility);

                win.HandleDeactivated();

                Assert.Equal(Visibility.Hidden, win.Visibility);

                win.AllowActualClose = true;
                win.Close();
            }
            catch (Exception ex)
            {
                threadEx = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (threadEx != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(threadEx).Throw();
    }

    [Fact]
    public void TranslationWindow_Deactivated_WhenCloseOnClickOutsideDisabled_DoesNotHideWindow()
    {
        Exception? threadEx = null;
        var thread = new Thread(() =>
        {
            try
            {
                var app = System.Windows.Application.Current ?? new System.Windows.Application();
                if (!app.Resources.MergedDictionaries.Any())
                {
                    app.Resources.MergedDictionaries.Add(new ResourceDictionary
                    {
                        Source = new Uri("pack://application:,,,/QuickTranslate;component/Themes/LightTheme.xaml", UriKind.Absolute)
                    });
                }

                var settingsService = Substitute.For<ISettingsService>();
                settingsService.CurrentSettings.Returns(new AppSettings
                {
                    CloseOnClickOutside = false
                });

                var transService = Substitute.For<ITranslationService>();
                var clipService = Substitute.For<IClipboardService>();
                var vm = new TranslationViewModel(
                    transService,
                    clipService,
                    settingsService,
                    NullLogger<TranslationViewModel>.Instance);

                var win = new TranslationWindow(vm, settingsService);
                win.Show();
                System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Loaded);

                Assert.Equal(Visibility.Visible, win.Visibility);

                win.HandleDeactivated();

                Assert.Equal(Visibility.Visible, win.Visibility);

                win.AllowActualClose = true;
                win.Close();
            }
            catch (Exception ex)
            {
                threadEx = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (threadEx != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(threadEx).Throw();
    }
}
