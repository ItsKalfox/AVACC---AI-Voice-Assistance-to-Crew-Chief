namespace AVACC.Core.Interfaces;

public interface ISoundEffectService
{
    /// <summary>
    /// Plays the PTT activation cue sound (start_ptt.wav) if enabled.
    /// </summary>
    void PlayPttStart();

    /// <summary>
    /// Plays the PTT release cue sound (end_ptt.wav) if enabled.
    /// </summary>
    void PlayPttEnd();

    /// <summary>
    /// Plays a specific sound file with optional volume and device override.
    /// </summary>
    void PlaySound(string soundName, float? volumeOverride = null, string? deviceIdOverride = null);
}
