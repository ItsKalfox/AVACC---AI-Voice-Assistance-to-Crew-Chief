using System.Diagnostics;
using System.Runtime.InteropServices;
using AVACC.Core.Configuration;
using AVACC.Core.Interfaces;
using AVACC.Core.Models;
using NAudio.CoreAudioApi;

namespace AVACC.Audio;

public class MicrophoneManager : IAudioEndpointService
{
    #region COM PolicyConfig Interop
    [ComImport, Guid("f8679f50-850a-41cf-9c72-430f290290c8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IPolicyConfig
    {
        [PreserveSig] int GetMixFormat(string pszDeviceName, IntPtr ppFormat);
        [PreserveSig] int GetDeviceFormat(string pszDeviceName, int bFlow, IntPtr ppFormat);
        [PreserveSig] int ResetDeviceFormat(string pszDeviceName);
        [PreserveSig] int SetDeviceFormat(string pszDeviceName, IntPtr ppEndpointFormat, IntPtr ppMixFormat);
        [PreserveSig] int GetProcessingPeriod(string pszDeviceName, int bDefault, IntPtr pPeriod, IntPtr pPeriodMin);
        [PreserveSig] int SetProcessingPeriod(string pszDeviceName, IntPtr pPeriod);
        [PreserveSig] int GetShareMode(string pszDeviceName, IntPtr pMode);
        [PreserveSig] int SetShareMode(string pszDeviceName, IntPtr pMode);
        [PreserveSig] int GetPropertyValue(string pszDeviceName, IntPtr pKey, IntPtr pv);
        [PreserveSig] int SetPropertyValue(string pszDeviceName, IntPtr pKey, IntPtr pv);
        [PreserveSig] int SetDefaultEndpoint([In, MarshalAs(UnmanagedType.LPWStr)] string wszDeviceId, int eRole);
        [PreserveSig] int SetEndpointVisibility(string pszDeviceName, int bVisible);
    }

    [ComImport, Guid("870af99c-171d-4f9e-af0d-e63df40c2bc9")]
    private class CPolicyConfigClient {}
    #endregion
    public bool ReconcileAudioDevices(AppSettings settings, out string statusMessage)
    {
        var messages = new List<string>();
        bool changed = false;

        try
        {
            var activeInputs = GetInputDevices();
            var activeOutputs = GetPlaybackDevices();

            // 1. Reconcile Microphone
            if (!string.IsNullOrEmpty(settings.SelectedAudioDeviceId))
            {
                var inputMatch = activeInputs.FirstOrDefault(d => string.Equals(d.Id, settings.SelectedAudioDeviceId, StringComparison.OrdinalIgnoreCase))
                              ?? activeInputs.FirstOrDefault(d => string.Equals(d.Name, settings.SelectedAudioDeviceName, StringComparison.OrdinalIgnoreCase));

                if (inputMatch == null)
                {
                    var defaultInput = activeInputs.FirstOrDefault(d => d.IsDefault) ?? activeInputs.FirstOrDefault();
                    if (defaultInput != null)
                    {
                        var oldName = settings.SelectedAudioDeviceName ?? settings.SelectedAudioDeviceId;
                        settings.SelectedAudioDeviceId = defaultInput.Id;
                        settings.SelectedAudioDeviceName = defaultInput.Name;
                        changed = true;
                        messages.Add($"Microphone '{oldName}' is unavailable; switched to default '{defaultInput.Name}'");
                    }
                }
            }
            else
            {
                var defaultInput = activeInputs.FirstOrDefault(d => d.IsDefault) ?? activeInputs.FirstOrDefault();
                if (defaultInput != null)
                {
                    settings.SelectedAudioDeviceId = defaultInput.Id;
                    settings.SelectedAudioDeviceName = defaultInput.Name;
                    changed = true;
                }
            }

            // 2. Reconcile PTT Sound Output Device
            if (!string.IsNullOrEmpty(settings.PttSoundOutputDeviceId))
            {
                var pttOutMatch = activeOutputs.FirstOrDefault(d => string.Equals(d.Id, settings.PttSoundOutputDeviceId, StringComparison.OrdinalIgnoreCase))
                               ?? activeOutputs.FirstOrDefault(d => string.Equals(d.Name, settings.PttSoundOutputDeviceName, StringComparison.OrdinalIgnoreCase));

                if (pttOutMatch == null)
                {
                    var defaultOut = activeOutputs.FirstOrDefault(d => d.IsDefault)
                                  ?? activeOutputs.FirstOrDefault(d => !d.Name.Contains("CABLE", StringComparison.OrdinalIgnoreCase))
                                  ?? activeOutputs.FirstOrDefault();
                    if (defaultOut != null)
                    {
                        var oldName = settings.PttSoundOutputDeviceName ?? settings.PttSoundOutputDeviceId;
                        settings.PttSoundOutputDeviceId = defaultOut.Id;
                        settings.PttSoundOutputDeviceName = defaultOut.Name;
                        changed = true;
                        messages.Add($"PTT sound output '{oldName}' is unavailable; switched to default '{defaultOut.Name}'");
                    }
                }
            }

            // 3. Reconcile Crew Chief Output Device
            if (!string.IsNullOrEmpty(settings.CrewChiefAudioOutputDeviceId))
            {
                var ccOutMatch = activeOutputs.FirstOrDefault(d => string.Equals(d.Id, settings.CrewChiefAudioOutputDeviceId, StringComparison.OrdinalIgnoreCase))
                              ?? activeOutputs.FirstOrDefault(d => string.Equals(d.Name, settings.CrewChiefAudioOutputDeviceName, StringComparison.OrdinalIgnoreCase));

                if (ccOutMatch == null)
                {
                    var fallbackOut = activeOutputs.FirstOrDefault(d => d.Name.Contains("CABLE", StringComparison.OrdinalIgnoreCase))
                                   ?? activeOutputs.FirstOrDefault(d => d.IsDefault)
                                   ?? activeOutputs.FirstOrDefault();
                    if (fallbackOut != null)
                    {
                        var oldName = settings.CrewChiefAudioOutputDeviceName ?? settings.CrewChiefAudioOutputDeviceId;
                        settings.CrewChiefAudioOutputDeviceId = fallbackOut.Id;
                        settings.CrewChiefAudioOutputDeviceName = fallbackOut.Name;
                        changed = true;
                        messages.Add($"Crew Chief bridge output '{oldName}' is unavailable; switched to '{fallbackOut.Name}'");
                    }
                }
            }

            // 4. Reconcile Crew Chief Launch Input Device
            if (!string.IsNullOrEmpty(settings.CrewChiefInputDeviceId))
            {
                var ccInMatch = activeInputs.FirstOrDefault(d => string.Equals(d.Id, settings.CrewChiefInputDeviceId, StringComparison.OrdinalIgnoreCase))
                             ?? activeInputs.FirstOrDefault(d => string.Equals(d.Name, settings.CrewChiefInputDeviceName, StringComparison.OrdinalIgnoreCase));

                if (ccInMatch == null)
                {
                    var fallbackIn = activeInputs.FirstOrDefault(d => d.Name.Contains("CABLE", StringComparison.OrdinalIgnoreCase))
                                  ?? activeInputs.FirstOrDefault(d => d.IsDefault)
                                  ?? activeInputs.FirstOrDefault();
                    if (fallbackIn != null)
                    {
                        var oldName = settings.CrewChiefInputDeviceName ?? settings.CrewChiefInputDeviceId;
                        settings.CrewChiefInputDeviceId = fallbackIn.Id;
                        settings.CrewChiefInputDeviceName = fallbackIn.Name;
                        changed = true;
                        messages.Add($"Crew Chief launch input device '{oldName}' is unavailable; switched to '{fallbackIn.Name}'");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            messages.Add($"Device reconciliation error: {ex.Message}");
        }

        statusMessage = string.Join("; ", messages);
        return changed;
    }

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

    public AudioDeviceInfo? GetDefaultInputDevice()
    {
        try
        {
            using var enumerator = new MMDeviceEnumerator();
            MMDevice? defaultDevice = null;
            try
            {
                defaultDevice = enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Console);
            }
            catch
            {
                try
                {
                    defaultDevice = enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Communications);
                }
                catch { }
            }

            if (defaultDevice != null)
            {
                return new AudioDeviceInfo
                {
                    Id = defaultDevice.ID,
                    Name = defaultDevice.FriendlyName,
                    IsDefault = true,
                    Channels = 1,
                    SampleRate = 16000
                };
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[MicrophoneManager] Failed to get default input device: {ex.Message}");
        }

        return null;
    }

    public bool SetDefaultInputDevice(string deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId) || string.Equals(deviceId, "default", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        try
        {
            var client = (IPolicyConfig)new CPolicyConfigClient();
            int hrConsole = client.SetDefaultEndpoint(deviceId, 0); // eConsole
            int hrMultimedia = client.SetDefaultEndpoint(deviceId, 1); // eMultimedia
            int hrComms = client.SetDefaultEndpoint(deviceId, 2); // eCommunications

            return hrConsole == 0 || hrMultimedia == 0 || hrComms == 0;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[MicrophoneManager] Failed to set default input device: {ex.Message}");
            return false;
        }
    }
}
