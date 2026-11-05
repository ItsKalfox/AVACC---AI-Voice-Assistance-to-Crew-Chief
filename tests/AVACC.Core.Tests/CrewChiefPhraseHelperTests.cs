using AVACC.Core.Helpers;
using Xunit;

namespace AVACC.Core.Tests;

public class CrewChiefPhraseHelperTests
{
    [Theory]
    [InlineData(0, "zero")]
    [InlineData(1, "one")]
    [InlineData(4, "four")]
    [InlineData(10, "ten")]
    [InlineData(14, "fourteen")]
    [InlineData(20, "twenty")]
    [InlineData(25, "twenty-five")]
    [InlineData(40, "fourty")] // Crew Chief spelling
    [InlineData(99, "ninety-nine")]
    public void NumberToWord_ConvertsNumbersCorrectly(int number, string expected)
    {
        var result = CrewChiefPhraseHelper.NumberToWord(number);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void FormatDigitsToWords_ReplacesDigitsWithWords()
    {
        var input = "what's position 4's last lap";
        var expected = "what's position four's last lap";

        var actual = CrewChiefPhraseHelper.FormatDigitsToWords(input);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("what's P4's last lap", 4)]
    [InlineData("what was position 2 last lap", 2)]
    [InlineData("how fast is P 10", 10)]
    [InlineData("who is in third place", 3)]
    [InlineData("gap to pos 5", 5)]
    [InlineData("who is 2nd", 2)]
    public void ExtractPositionNumber_ExtractsFromVariousPhrasings(string text, int expectedPosition)
    {
        var actual = CrewChiefPhraseHelper.ExtractPositionNumber(text);
        Assert.Equal(expectedPosition, actual);
    }

    [Theory]
    [InlineData("calculate fuel for 20 laps", 20, "laps")]
    [InlineData("how much fuel for 45 minutes", 45, "minutes")]
    [InlineData("fuel for 1 hour", 1, "hours")]
    [InlineData("calculate fuel for twenty-five laps", 25, "laps")]
    public void ExtractFuelCalculationParameters_ExtractsAmountAndUnit(string text, int expectedAmount, string expectedUnit)
    {
        var (amount, unit) = CrewChiefPhraseHelper.ExtractFuelCalculationParameters(text);
        Assert.Equal(expectedAmount, amount);
        Assert.Equal(expectedUnit, unit);
    }

    [Fact]
    public void ResolvePhrase_ResolvesDynamicPositionLastLap()
    {
        var template = "what's position {number}'s last lap";
        var transcript = "What was P4's last lap time?";

        var resolved = CrewChiefPhraseHelper.ResolvePhrase("WHATS_POSITION_LAST_LAP", null, transcript, template);
        Assert.Equal("what's position four's last lap", resolved);
    }

    [Fact]
    public void ResolvePhrase_ResolvesDynamicFuelCalculation()
    {
        var template = "calculate fuel for {amount} {unit}";
        var transcript = "How much fuel do I need for 25 laps?";

        var resolved = CrewChiefPhraseHelper.ResolvePhrase("CALCULATE_FUEL_FOR", null, transcript, template);
        Assert.Equal("calculate fuel for twenty-five laps", resolved);
    }

    [Fact]
    public void ResolvePhrase_ResolvesWhoIsInPosition()
    {
        var template = "who's in position {number}";
        var transcript = "Who is currently in P3?";

        var resolved = CrewChiefPhraseHelper.ResolvePhrase("WHOS_IN_POSITION", null, transcript, template);
        Assert.Equal("who's in position three", resolved);
    }

    [Fact]
    public void ResolvePhrase_PreservesStaticVoiceLine_WhenNoPlaceholders()
    {
        var template = "can you hear me";
        var transcript = "Radio check, can you hear me?";

        var resolved = CrewChiefPhraseHelper.ResolvePhrase("RADIO_CHECK", null, transcript, template);
        Assert.Equal("can you hear me", resolved);
    }

    [Fact]
    public void ResolvePhrase_UsesLlmProvidedSpokenPhrase_IfPresent()
    {
        var template = "what's position {number}'s last lap";
        var transcript = "What was P4's lap?";
        var llmSpokenPhrase = "what's position 4's last lap";

        var resolved = CrewChiefPhraseHelper.ResolvePhrase("WHATS_POSITION_LAST_LAP", llmSpokenPhrase, transcript, template);
        Assert.Equal("what's position four's last lap", resolved);
    }

    [Fact]
    public void ResolvePhrase_PreservesStaticVoiceLine_EvenWhenLlmReturnsDifferentSpokenPhrase()
    {
        var template = "don't talk in the corners";
        var transcript = "no messages in the braking";
        var llmSpokenPhrase = "no breaking under cornering";

        var resolved = CrewChiefPhraseHelper.ResolvePhrase("DONT_TALK_IN_THE_CORNERS", llmSpokenPhrase, transcript, template);
        Assert.Equal("don't talk in the corners", resolved);
    }

    [Fact]
    public void ResolvePhrase_FallsBackToLlmSpokenPhrase_OnlyWhenNoTemplateConfigured()
    {
        var transcript = "give me corner silence";
        var llmSpokenPhrase = "don't talk in the corners";

        var resolved = CrewChiefPhraseHelper.ResolvePhrase("DONT_TALK_IN_THE_CORNERS", llmSpokenPhrase, transcript, null);
        Assert.Equal("don't talk in the corners", resolved);
    }
}

