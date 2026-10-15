namespace QuickTranslate.Services;

public interface IClipboardService
{
    Task<string?> GetSelectedTextAsync(CancellationToken cancellationToken = default);
    Task SetTextAsync(string text);
}
