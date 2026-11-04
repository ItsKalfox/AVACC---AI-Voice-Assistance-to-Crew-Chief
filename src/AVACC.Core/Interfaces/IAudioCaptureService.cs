using AVACC.Core.Models;

namespace AVACC.Core.Interfaces;

public interface IAudioCaptureService
{
    Task<IReadOnlyList<AudioDeviceInfo>> GetAvailableDevicesAsync();
    Task StartRecordingAsync(string? deviceId, int sampleRate, int channels, CancellationToken cancellationToken = default);
    Task<byte[]> StopRecordingAsync();
    bool IsRecording { get; }
    float CurrentInputLevel { get; }
    TimeSpan CurrentRecordingDuration { get; }
    event EventHandler<float>? InputLevelChanged;
    event EventHandler<TimeSpan>? RecordingDurationChanged;
    event EventHandler<string>? RecordingError;
}

