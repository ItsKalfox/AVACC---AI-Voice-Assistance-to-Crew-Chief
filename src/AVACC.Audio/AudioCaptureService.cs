using System.Diagnostics;
using AVACC.Core.Interfaces;
using AVACC.Core.Models;
using Microsoft.Extensions.Logging;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace AVACC.Audio;

public class AudioCaptureService : IAudioCaptureService, IDisposable
{
    private readonly MicrophoneManager _microphoneManager;
    private readonly ILogger<AudioCaptureService>? _logger;
    private WasapiCapture? _capture;
    private MemoryStream? _recordingStream;
    private WaveFileWriter? _waveWriter;
    private readonly Stopwatch _stopwatch = new();
    private bool _isRecording;
    private float _currentLevel;

    public bool IsRecording => _isRecording;
    public float CurrentInputLevel => _currentLevel;
    public TimeSpan CurrentRecordingDuration => _stopwatch.Elapsed;

    public event EventHandler<float>? InputLevelChanged;
    public event EventHandler<TimeSpan>? RecordingDurationChanged;
    public event EventHandler<string>? RecordingError;

    public AudioCaptureService(MicrophoneManager microphoneManager, ILogger<AudioCaptureService>? logger = null)
    {
        _microphoneManager = microphoneManager;
        _logger = logger;
    }

    public Task<IReadOnlyList<AudioDeviceInfo>> GetAvailableDevicesAsync()
    {
        return Task.FromResult(_microphoneManager.GetInputDevices());
    }

    public Task StartRecordingAsync(string? deviceId, int sampleRate, int channels, CancellationToken cancellationToken = default)
    {
        if (_isRecording)
        {
            _logger?.LogWarning("Recording already in progress.");
            return Task.CompletedTask;
        }

        try
        {
            _recordingStream = new MemoryStream();
            MMDevice? selectedDevice = null;

            using (var enumerator = new MMDeviceEnumerator())
            {
                if (!string.IsNullOrEmpty(deviceId))
                {
                    try
                    {
                        selectedDevice = enumerator.GetDevice(deviceId);
                    }
                    catch
                    {
                        _logger?.LogWarning("Specified device {DeviceId} not found, falling back to default", deviceId);
                    }
                }

                if (selectedDevice == null)
                {
                    try
                    {
                        selectedDevice = enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Multimedia);
                    }
                    catch
                    {
                        try
                        {
                            selectedDevice = enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Communications);
                        }
                        catch
                        {
                            selectedDevice = enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Console);
                        }
                    }
                }

                _capture = new WasapiCapture(selectedDevice);
            }

            // Write captured audio in the device's native format
            _waveWriter = new WaveFileWriter(_recordingStream, _capture.WaveFormat);

            var is32Bit = _capture.WaveFormat.Encoding == WaveFormatEncoding.IeeeFloat || _capture.WaveFormat.BitsPerSample == 32;

            _capture.DataAvailable += (s, e) =>
            {
                if (!_isRecording || _waveWriter == null) return;

                _waveWriter.Write(e.Buffer, 0, e.BytesRecorded);

                // Calculate peak level for level meter
                var maxSample = 0f;
                if (is32Bit)
                {
                    for (var index = 0; index <= e.BytesRecorded - 4; index += 4)
                    {
                        var sample = Math.Abs(BitConverter.ToSingle(e.Buffer, index));
                        if (sample > maxSample) maxSample = sample;
                    }
                }
                else
                {
                    for (var index = 0; index <= e.BytesRecorded - 2; index += 2)
                    {
                        var sample = (short)((e.Buffer[index + 1] << 8) | e.Buffer[index]);
                        var sample32 = Math.Abs(sample / 32768f);
                        if (sample32 > maxSample) maxSample = sample32;
                    }
                }

                // Apply mild visual boost for meter responsiveness
                _currentLevel = Math.Clamp(maxSample * 1.5f, 0f, 1f);
                InputLevelChanged?.Invoke(this, _currentLevel);
                RecordingDurationChanged?.Invoke(this, _stopwatch.Elapsed);
            };

            _capture.RecordingStopped += (s, e) =>
            {
                if (e.Exception != null)
                {
                    _logger?.LogError(e.Exception, "Audio recording stopped with error");
                    RecordingError?.Invoke(this, e.Exception.Message);
                }
            };

            _capture.StartRecording();
            _stopwatch.Restart();
            _isRecording = true;
            _logger?.LogInformation("Started WASAPI microphone recording");
        }
        catch (Exception ex)
        {
            _isRecording = false;
            _logger?.LogError(ex, "Failed to start audio recording");
            RecordingError?.Invoke(this, ex.Message);
            throw;
        }

        return Task.CompletedTask;
    }

    public async Task<byte[]> StopRecordingAsync()
    {
        if (!_isRecording && _recordingStream == null)
        {
            return Array.Empty<byte>();
        }

        try
        {
            _stopwatch.Stop();
            _isRecording = false;

            if (_capture != null)
            {
                _capture.StopRecording();
                _capture.Dispose();
                _capture = null;
            }

            if (_waveWriter != null)
            {
                await _waveWriter.FlushAsync();
                _waveWriter.Dispose();
                _waveWriter = null;
            }

            _currentLevel = 0f;
            InputLevelChanged?.Invoke(this, 0f);

            var audioData = _recordingStream?.ToArray() ?? Array.Empty<byte>();
            _recordingStream?.Dispose();
            _recordingStream = null;

            _logger?.LogInformation("Stopped audio recording, captured {Bytes} bytes ({Duration}s)",
                audioData.Length, _stopwatch.Elapsed.TotalSeconds);

            return audioData;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error while stopping audio recording");
            return Array.Empty<byte>();
        }
    }

    public void Dispose()
    {
        _capture?.Dispose();
        _waveWriter?.Dispose();
        _recordingStream?.Dispose();
    }
}

