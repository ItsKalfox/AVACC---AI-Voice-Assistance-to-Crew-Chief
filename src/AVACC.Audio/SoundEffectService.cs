using System.Collections.Concurrent;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using AVACC.Core.Interfaces;

namespace AVACC.Audio;

public class SoundEffectService : ISoundEffectService
{
    private readonly ISettingsService _settingsService;
    private readonly IEventLogService _eventLogService;
    private readonly ConcurrentDictionary<string, byte[]> _soundCache = new(StringComparer.OrdinalIgnoreCase);

    public SoundEffectService(
        ISettingsService settingsService,
        IEventLogService eventLogService,
        IPushToTalkService? pttService = null)
    {
        _settingsService = settingsService;
        _eventLogService = eventLogService;

        if (pttService != null)
        {
            pttService.PttPressed += (s, e) => PlayPttStart();
            pttService.PttReleased += (s, e) => PlayPttEnd();
        }

        // Preload sounds asynchronously on startup
        Task.Run(() =>
        {
            GetSoundData("start_ptt.wav");
            GetSoundData("end_ptt.wav");
        });
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

    public void PlaySound(string soundName, float? volumeOverride = null, string? deviceIdOverride = null)
    {
        var settings = _settingsService.CurrentSettings;
        var volume = volumeOverride ?? settings.PttSoundVolume;
        var deviceId = deviceIdOverride ?? settings.PttSoundOutputDeviceId;
        PlaySoundInternal(soundName, volume, deviceId);
    }

    private void PlaySoundInternal(string soundName, float volume, string? deviceId)
    {
        Task.Run(() =>
        {
            try
            {
                var soundData = GetSoundData(soundName);
                if (soundData == null || soundData.Length == 0)
                {
                    _eventLogService.LogWarning($"PTT cue sound '{soundName}' not found in fx-sound folder.");
                    return;
                }

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

                var ms = new MemoryStream(soundData);
                var waveReader = new WaveFileReader(ms);
                var sampleProvider = waveReader.ToSampleProvider();

                var volumeProvider = new VolumeSampleProvider(sampleProvider)
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
                        waveReader.Dispose();
                        ms.Dispose();
                    }
                    catch { }
                };

                wasapiOut.Play();
            }
            catch (Exception ex)
            {
                _eventLogService.LogWarning($"Failed to play sound '{soundName}': {ex.Message}");
            }
        });
    }

    private byte[]? GetSoundData(string fileName)
    {
        if (_soundCache.TryGetValue(fileName, out var cached))
        {
            return cached;
        }

        var loaded = LoadSoundBytes(fileName);
        if (loaded != null && loaded.Length > 0)
        {
            _soundCache[fileName] = loaded;
        }
        return loaded;
    }

    private static byte[]? LoadSoundBytes(string fileName)
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
                    try
                    {
                        return File.ReadAllBytes(fullPath);
                    }
                    catch { }
                }
            }
        }

        return null;
    }
}
