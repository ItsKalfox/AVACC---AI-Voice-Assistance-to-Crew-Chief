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
    /// Plays a random native Crew Chief "didn't understand" audio clip matching the active chief persona.
    /// </summary>
    void PlayDidntUnderstand();

    /// <summary>
    /// Plays the AI model / network error alert sound cue if enabled.
    /// </summary>
    void PlayAiError();

    /// <summary>
    /// Plays a specific sound file with optional volume and device override.
    /// </summary>
    void PlaySound(string soundName, float? volumeOverride = null, string? deviceIdOverride = null);

    /// <summary>
    /// Discovers available Chief voice personas installed on the computer.
    /// </summary>
    IReadOnlyList<string> GetAvailableChiefPersonas();

    /// <summary>
    /// Automatically detects the currently active Chief persona from Crew Chief's user preferences.
    /// </summary>
    string DetectActiveChiefPersona();
}

