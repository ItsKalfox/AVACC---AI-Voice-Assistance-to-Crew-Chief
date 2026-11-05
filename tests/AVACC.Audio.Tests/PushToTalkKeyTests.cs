using AVACC.Audio;
using Xunit;

namespace AVACC.Audio.Tests;

public class PushToTalkKeyTests
{
    [Theory]
    [InlineData(0x21, 0, "PageUp")]
    [InlineData(0x22, 0, "PageDown")]
    [InlineData(0x11, 0, "LeftCtrl")]
    [InlineData(0x11, 1, "RightCtrl")]
    [InlineData(0xA3, 0, "RightCtrl")]
    [InlineData(0x70, 0, "F1")]
    [InlineData(0x56, 0, "V")]
    [InlineData(0x20, 0, "Space")]
    [InlineData(0x14, 0, "CapsLock")]
    public void VkCodeToKeyName_TranslatesKnownVirtualKeys(int vkCode, int flags, string expectedName)
    {
        var result = PushToTalkService.VkCodeToKeyName(vkCode, flags);
        Assert.Equal(expectedName, result);
    }

    [Theory]
    [InlineData(0x21, 0, "PageUp", true)]
    [InlineData(0x21, 0, "RightCtrl", false)]
    [InlineData(0x11, 1, "RightCtrl", true)]
    [InlineData(0x11, 0, "RightCtrl", false)]
    [InlineData(0x11, 0, "LeftCtrl", true)]
    [InlineData(0x56, 0, "V", true)]
    [InlineData(0x56, 0, "v", true)]
    [InlineData(0x20, 0, "Space", true)]
    public void MatchesKey_AccuratelyDistinguishesKeys(int vkCode, int flags, string configuredKey, bool expectedMatch)
    {
        var match = PushToTalkService.MatchesKey(vkCode, flags, configuredKey);
        Assert.Equal(expectedMatch, match);
    }
}
