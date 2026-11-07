using AVACC.Core.Models;

namespace AVACC.Core.Interfaces;

public interface IAudioEndpointService
{
    IReadOnlyList<AudioDeviceInfo> GetInputDevices();
    IReadOnlyList<AudioDeviceInfo> GetPlaybackDevices();
    AudioDeviceInfo? GetDefaultInputDevice();
    bool SetDefaultInputDevice(string deviceId);
}
