using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using AVACC.Core.Interfaces;

namespace AVACC.Audio;

public class SoundEffectService : ISoundEffectService
{
    private readonly ISettingsService _settingsService;
    private readonly IEventLogService _eventLogService;
    private readonly ICrewChiefMonitorService? _crewChiefMonitor;

    public SoundEffectService(
        ISettingsService settingsService,
        IEventLogService eventLogService,
        IPushToTalkService? pttService = null,
        ICrewChiefMonitorService? crewChiefMonitor = null)
    {
        _settingsService = settingsService;
        _eventLogService = eventLogService;
        _crewChiefMonitor = crewChiefMonitor;

        if (pttService != null)
        {
            pttService.PttPressed += (s, e) =>
            {
                var settings = _settingsService.CurrentSettings;
                if (settings.PlayErrorSoundWhenCrewChiefNotRunning && _crewChiefMonitor != null && !_crewChiefMonitor.IsRunning)
                {
                    PlayCrewChiefNotRunningError();
                    return;
                }

                PlayPttStart();
            };

            pttService.PttReleased += (s, e) =>
            {
                var settings = _settingsService.CurrentSettings;
                if (settings.PlayErrorSoundWhenCrewChiefNotRunning && _crewChiefMonitor != null && !_crewChiefMonitor.IsRunning)
                {
                    return;
                }

                PlayPttEnd();
            };
        }
    }

    public void PlayPttStart()
    {
        var settings = _settingsService.CurrentSettings;
        if (!settings.EnablePttStartSound) return;

        PlaySoundInternal("start_ptt.wav", settings.PttSoundVolume, settings.PttSoundOutputDeviceId);
    }

    public void PlayPttEnd()
    {
        var settings = _settingsService.CurrentSettings;
        if (!settings.EnablePttEndSound) return;

        PlaySoundInternal("end_ptt.wav", settings.PttSoundVolume, settings.PttSoundOutputDeviceId);
    }

    public void PlayDidntUnderstand()
    {
        var settings = _settingsService.CurrentSettings;
        if (!settings.EnableDidntUnderstandSound) return;

        Task.Run(() =>
        {
            try
            {
                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var selected = settings.SelectedChiefVoicePersona;
                var persona = (string.IsNullOrEmpty(selected) || string.Equals(selected, "Auto-Detect", StringComparison.OrdinalIgnoreCase))
                    ? DetectActiveChiefPersona()
                    : selected;

                string targetDir;
                if (string.Equals(persona, "Jim", StringComparison.OrdinalIgnoreCase))
                {
                    targetDir = Path.Combine(localAppData, "CrewChiefV4", "Sounds", "voice", "acknowledge", "didnt_understand");
                }
                else
                {
                    targetDir = Path.Combine(localAppData, "CrewChiefV4", "Sounds", "alt", persona, "voice", "acknowledge", "didnt_understand");
                    if (!Directory.Exists(targetDir))
                    {
                        targetDir = Path.Combine(localAppData, "CrewChiefV4", "Sounds", "voice", "acknowledge", "didnt_understand");
                    }
                }

                if (!Directory.Exists(targetDir))
                {
                    _eventLogService.LogWarning($"Crew Chief 'didn't understand' folder not found at '{targetDir}'");
                    return;
                }

                var files = Directory.GetFiles(targetDir, "*.wav");
                if (files.Length == 0)
                {
                    _eventLogService.LogWarning($"No .wav files found in '{targetDir}'");
                    return;
                }

                var randomFile = files[Random.Shared.Next(files.Length)];
                _eventLogService.LogInfo($"Playing Crew Chief 'didn't understand' ({persona}): {Path.GetFileName(randomFile)}");
                PlayFileInternal(randomFile, settings.PttSoundVolume, settings.PttSoundOutputDeviceId);
            }
            catch (Exception ex)
            {
                _eventLogService.LogWarning($"Failed to play 'didn't understand' sound: {ex.Message}");
            }
        });
    }

    public void PlayAiError()
    {
        var settings = _settingsService.CurrentSettings;
        if (!settings.EnableAiErrorSound) return;

        PlaySoundInternal("error.mp3", settings.PttSoundVolume, settings.PttSoundOutputDeviceId);
    }

    public void PlayCrewChiefNotRunningError()
    {
        var settings = _settingsService.CurrentSettings;
        if (!settings.PlayErrorSoundWhenCrewChiefNotRunning) return;

        PlaySoundInternal("error.mp3", settings.PttSoundVolume, settings.PttSoundOutputDeviceId);
    }

    public void PlaySound(string soundName, float? volumeOverride = null, string? deviceIdOverride = null)
    {
        var settings = _settingsService.CurrentSettings;
        var volume = volumeOverride ?? settings.PttSoundVolume;
        var deviceId = deviceIdOverride ?? settings.PttSoundOutputDeviceId;
        PlaySoundInternal(soundName, volume, deviceId);
    }

    public IReadOnlyList<string> GetAvailableChiefPersonas()
    {
        var personas = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Jim" };
        try
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var altDir = Path.Combine(localAppData, "CrewChiefV4", "Sounds", "alt");
            if (Directory.Exists(altDir))
            {
                foreach (var dir in Directory.GetDirectories(altDir))
                {
                    var name = Path.GetFileName(dir);
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        personas.Add(name);
                    }
                }
            }
        }
        catch { }

        var list = new List<string> { "Auto-Detect" };
        list.AddRange(personas.OrderBy(p => p, StringComparer.OrdinalIgnoreCase));
        return list;
    }

    public string DetectActiveChiefPersona()
    {
        try
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var brittonDir = Path.Combine(localAppData, "Britton_IT_Ltd");
            if (Directory.Exists(brittonDir))
            {
                var configFiles = Directory.GetFiles(brittonDir, "user.config", SearchOption.AllDirectories)
                    .OrderByDescending(File.GetLastWriteTimeUtc);

                foreach (var configFile in configFiles)
                {
                    var content = File.ReadAllText(configFile);
                    var match = Regex.Match(
                        content,
                        @"<setting\s+name=""chief_name""\s+serializeAs=""String"">\s*<value>([^<]+)</value>",
                        RegexOptions.IgnoreCase);

                    if (match.Success && !string.IsNullOrWhiteSpace(match.Groups[1].Value))
                    {
                        return match.Groups[1].Value.Trim();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _eventLogService.LogWarning($"Failed to auto-detect active Chief persona: {ex.Message}");
        }

        return "Jim";
    }

    private void PlaySoundInternal(string soundName, float volume, string? deviceId)
    {
        var path = ResolveSoundFilePath(soundName);
        if (string.IsNullOrEmpty(path))
        {
            _eventLogService.LogWarning($"Sound cue '{soundName}' not found in fx-sound folder.");
            return;
        }

        PlayFileInternal(path, volume, deviceId);
    }

    private void PlayFileInternal(string filePath, float volume, string? deviceId)
    {
        Task.Run(() =>
        {
            try
            {
                using var enumerator = new MMDeviceEnumerator();
                MMDevice? targetDevice = null;

                if (!string.IsNullOrEmpty(deviceId) && deviceId != "default")
                {
                    try
                    {
                        targetDevice = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)
                            .FirstOrDefault(d => string.Equals(d.ID, deviceId, StringComparison.OrdinalIgnoreCase));
                    }
                    catch { }
                }

                targetDevice ??= enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

                var audioReader = new AudioFileReader(filePath);
                var volumeProvider = new VolumeSampleProvider(audioReader)
                {
                    Volume = Math.Clamp(volume, 0.0f, 1.0f)
                };

                var wasapiOut = new WasapiOut(targetDevice, AudioClientShareMode.Shared, false, 50);
                wasapiOut.Init(volumeProvider.ToWaveProvider());

                wasapiOut.PlaybackStopped += (s, e) =>
                {
                    try
                    {
                        wasapiOut.Dispose();
                        audioReader.Dispose();
                    }
                    catch { }
                };

                wasapiOut.Play();
            }
            catch (Exception ex)
            {
                _eventLogService.LogWarning($"Failed to play audio '{filePath}': {ex.Message}");
            }
        });
    }

    private static string? ResolveSoundFilePath(string fileName)
    {
        var alternatives = new List<string> { fileName };
        if (fileName.Contains('_')) alternatives.Add(fileName.Replace('_', '-'));
        if (fileName.Contains('-')) alternatives.Add(fileName.Replace('-', '_'));

        var candidateDirs = new[]
        {
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "fx-sound"),
            Path.Combine(AppContext.BaseDirectory, "fx-sound"),
            Path.Combine(Directory.GetCurrentDirectory(), "fx-sound"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "fx-sound"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "fx-sound")
        };

        foreach (var dir in candidateDirs)
        {
            if (!Directory.Exists(dir)) continue;
            foreach (var alt in alternatives)
            {
                var fullPath = Path.Combine(dir, alt);
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }
        }

        return null;
    }
}
