namespace AVACC.Core.Interfaces;

public interface IPushToTalkService : IDisposable
{
    bool IsActive { get; }
    string ConfiguredKey { get; }
    void Initialize(string keyBinding, bool isGlobal, bool disableWhenInactive);
    void UpdateConfiguration(string keyBinding, bool isGlobal, bool disableWhenInactive);
    void TriggerPttDown();
    void TriggerPttUp();
    event EventHandler? PttPressed;
    event EventHandler? PttReleased;
}

