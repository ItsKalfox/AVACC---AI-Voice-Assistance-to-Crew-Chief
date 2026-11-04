using AVACC.Core.Models;
using NAudio.CoreAudioApi;

namespace AVACC.Audio;

public class MicrophoneManager
{
    public IReadOnlyList<AudioDeviceInfo> GetInputDevices()
    {
        var devices = new List<AudioDeviceInfo>();

        try
        {
            using var enumerator = new MMDeviceEnumerator();
            var endpoints = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);

            MMDevice? defaultDevice = null;
            try
            {
                defaultDevice = enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Communications);
            }
            catch
            {
                // Fall back if no default communications device
                try
                {
                    defaultDevice = enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Console);
                }
                catch
                {
                    // No default device
                }
            }

            foreach (var endpoint in endpoints)
            {
                var isDefault = defaultDevice != null && endpoint.ID == defaultDevice.ID;
                var sampleRate = 16000;
                var channels = 1;

                try
                {
                    sampleRate = endpoint.AudioClient.MixFormat.SampleRate;
                    channels = endpoint.AudioClient.MixFormat.Channels;
                }
                catch
                {
                    // Ignore format inspection failure
                }

                devices.Add(new AudioDeviceInfo
                {
                    Id = endpoint.ID,
                    Name = endpoint.FriendlyName,
                    IsDefault = isDefault,
                    Channels = channels,
                    SampleRate = sampleRate
                });
            }
        }
        catch
        {
            // If WASAPI audio enumeration fails, return a fallback default
            devices.Add(new AudioDeviceInfo
            {
                Id = "default",
                Name = "Default System Microphone",
                IsDefault = true,
                Channels = 1,
                SampleRate = 16000
            });
        }

        return devices;
    }

    public IReadOnlyList<AudioDeviceInfo> GetPlaybackDevices()
    {
        var devices = new List<AudioDeviceInfo>();

        try
        {
            using var enumerator = new MMDeviceEnumerator();
            var endpoints = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);

            MMDevice? defaultDevice = null;
            try
            {
                defaultDevice = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            }
            catch { }

            foreach (var endpoint in endpoints)
            {
                bool isDefault = defaultDevice != null && string.Equals(endpoint.ID, defaultDevice.ID, StringComparison.OrdinalIgnoreCase);
                int channels = 2;
                int sampleRate = 44100;
                try
                {
                    var mix = endpoint.AudioClient?.MixFormat;
                    if (mix != null)
                    {
                        channels = mix.Channels;
                        sampleRate = mix.SampleRate;
                    }
                }
                catch { }

                devices.Add(new AudioDeviceInfo
                {
                    Id = endpoint.ID,
                    Name = endpoint.FriendlyName,
                    IsDefault = isDefault,
                    Channels = channels,
                    SampleRate = sampleRate
                });
            }
        }
        catch
        {
            devices.Add(new AudioDeviceInfo
            {
                Id = "default",
                Name = "Default Windows Playback Device",
                IsDefault = true,
                Channels = 2,
                SampleRate = 44100
            });
        }

        return devices;
    }
}
