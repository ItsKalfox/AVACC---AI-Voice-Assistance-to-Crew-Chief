using AVACC.Audio;
using Xunit;

namespace AVACC.Audio.Tests;

public class AudioSubsystemTests
{
    [Fact]
    public void MicrophoneManager_EnumeratesDevices_OrReturnsSafeFallback()
    {
        var manager = new MicrophoneManager();
        var devices = manager.GetInputDevices();

        Assert.NotNull(devices);
        Assert.NotEmpty(devices);
        Assert.All(devices, d =>
        {
            Assert.False(string.IsNullOrWhiteSpace(d.Id));
            Assert.False(string.IsNullOrWhiteSpace(d.Name));
            Assert.True(d.SampleRate > 0);
            Assert.True(d.Channels > 0);
        });
    }

    [Fact]
    public void AudioDiagnostics_GeneratesReadableReport()
    {
        var report = AudioDiagnostics.GetSystemAudioReport();

        Assert.NotNull(report);
        Assert.Contains("AVACC Audio Subsystem Diagnostic Report", report);
        Assert.Contains("Active Capture Endpoints", report);
    }

    [Fact]
    public void PushToTalkService_InitializesConfig_Correctly()
    {
        using var ptt = new PushToTalkService();
        ptt.Initialize("RightCtrl", false, false);

        Assert.Equal("RightCtrl", ptt.ConfiguredKey);
        Assert.False(ptt.IsActive); // not registered when isGlobal is false
    }

    [Fact]
    public void PushToTalkService_UpdatesConfig_Correctly()
    {
        using var ptt = new PushToTalkService();
        ptt.Initialize("Space", false, false);
        Assert.Equal("Space", ptt.ConfiguredKey);

        ptt.UpdateConfiguration("F12", false, false);
        Assert.Equal("F12", ptt.ConfiguredKey);
    }

    [Fact]
    public void PushToTalkService_TriggerPttDownAndUp_FiresEvents()
    {
        using var ptt = new PushToTalkService();
        ptt.Initialize("RightCtrl", false, false);

        var pressedFired = false;
        var releasedFired = false;

        ptt.PttPressed += (s, e) => pressedFired = true;
        ptt.PttReleased += (s, e) => releasedFired = true;

        ptt.TriggerPttDown();
        Assert.True(pressedFired);
        Assert.False(releasedFired);

        ptt.TriggerPttUp();
        Assert.True(releasedFired);
    }
}

