namespace QuickTranslate.Services;

public interface IStartupService
{
    bool IsAutoStartEnabled();
    void SetAutoStart(bool enable);
}
