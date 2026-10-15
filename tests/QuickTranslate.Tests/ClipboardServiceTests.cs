using Microsoft.Extensions.Logging.Abstractions;
using QuickTranslate.Services;
using Xunit;

namespace QuickTranslate.Tests;

public class ClipboardServiceTests
{
    [Fact]
    public async Task GetSelectedTextAsync_WhenCancelled_ReturnsPromptly()
    {
        var service = new ClipboardService(NullLogger<ClipboardService>.Instance);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var startTime = DateTime.UtcNow;
        var result = await service.GetSelectedTextAsync(cts.Token);
        var duration = DateTime.UtcNow - startTime;

        // When cancelled immediately, it should return very quickly without long delays
        Assert.Null(result);
        Assert.True(duration.TotalMilliseconds < 500, $"Expected duration < 500ms, but was {duration.TotalMilliseconds}ms");
    }

    [Fact]
    public async Task GetSelectedTextAsync_WhenNoSelection_CompletesInShortTime()
    {
        var service = new ClipboardService(NullLogger<ClipboardService>.Instance);

        var startTime = DateTime.UtcNow;
        var result = await service.GetSelectedTextAsync();
        var duration = DateTime.UtcNow - startTime;

        // In headless / test environment without focused window with selection,
        // it should complete well under 500ms (previously took ~1000ms)
        Assert.True(duration.TotalMilliseconds < 500, $"Expected duration < 500ms, but was {duration.TotalMilliseconds}ms");
    }

    [Fact]
    public async Task SetTextAsync_WithValidText_ExecutesWithoutError()
    {
        var service = new ClipboardService(NullLogger<ClipboardService>.Instance);
        await service.SetTextAsync("QuickTranslate Test");
    }
}
