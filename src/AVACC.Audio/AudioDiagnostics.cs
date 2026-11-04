using NAudio.CoreAudioApi;

namespace AVACC.Audio;

public class AudioDiagnostics
{
    public static string GetSystemAudioReport()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== AVACC Audio Subsystem Diagnostic Report ===");
        sb.AppendLine($"Report Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

        try
        {
            using var enumerator = new MMDeviceEnumerator();
            var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);

            sb.AppendLine($"Active Capture Endpoints: {devices.Count}");
            foreach (var dev in devices)
            {
                sb.AppendLine($"- Friendly Name: {dev.FriendlyName}");
                sb.AppendLine($"  ID: {dev.ID}");
                sb.AppendLine($"  State: {dev.State}");
                try
                {
                    sb.AppendLine($"  Mix Format: {dev.AudioClient.MixFormat.SampleRate}Hz, {dev.AudioClient.MixFormat.Channels}ch, {dev.AudioClient.MixFormat.BitsPerSample}-bit");
                }
                catch
                {
                    sb.AppendLine("  Mix Format: Could not inspect");
                }
            }
        }
        catch (Exception ex)
        {
            sb.AppendLine($"WASAPI Enumeration Error: {ex.Message}");
        }

        return sb.ToString();
    }
}
