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
    [InlineData("add 55 liters", 55, "litres")]
    [InlineData("add 55 litres", 55, "litres")]
    [InlineData("please add 50 liters in the pit stop menu", 50, "litres")]
    [InlineData("add 60 liters", 60, "litres")]
    [InlineData("add 70 litres", 70, "litres")]
    [InlineData("add 30 liters", 30, "litres")]
    [InlineData("add fifty-five liters", 55, "litres")]
    [InlineData("add sixty litres", 60, "litres")]
    [InlineData("pit stop add 40", 40, null)]
    [InlineData("add 35", 35, null)]
    public void ExtractFuelCalculationParameters_ExtractsAmountAndUnit(string text, int expectedAmount, string? expectedUnit)
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
    public void ResolvePhrase_ResolvesPitStopAddFuel_NormalizingToBritishLitres()
    {
        // Even if user's configured template uses American spelling "liters",
        // it must resolve to British "litres" which Crew Chief speech recognition expects
        var template = "Add Fuel, {amount} liters";
        var transcript = "Please add 55 liters in the pit stop menu";

        var resolved = CrewChiefPhraseHelper.ResolvePhrase("PIT_STOP_ADD", null, transcript, template);
        Assert.Equal("Add Fuel, fifty-five litres", resolved);
    }

    [Theory]
    [InlineData("Add Fuel, 50 liters", "Add Fuel, 50 litres")]
    [InlineData("Pit stop add 60 liters", "Pit stop add 60 litres")]
    [InlineData("calculate fuel for 30 liters", "calculate fuel for 30 litres")]
    public void NormalizeCrewChiefBritishTerms_ConvertsAmericanLitersToLitres(string input, string expected)
    {
        var actual = CrewChiefPhraseHelper.NormalizeCrewChiefBritishTerms(input);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("Please add 50 liters in the pit stop menu", "Add Fuel, fifty litres")]
    [InlineData("Add 55 liters", "Add Fuel, fifty-five litres")]
    [InlineData("Add 60 litres", "Add Fuel, sixty litres")]
    [InlineData("Pit stop add 70 liters", "Add Fuel, seventy litres")]
    [InlineData("Add 30", "Add Fuel, thirty litres")]
    [InlineData("Add fifty-five litres", "Add Fuel, fifty-five litres")]
    public void ResolvePhrase_ResolvesVariousPitStopFuelAmounts(string transcript, string expectedPhrase)
    {
        var template = "Add Fuel, {amount} litres";
        var resolved = CrewChiefPhraseHelper.ResolvePhrase("PIT_STOP_ADD", null, transcript, template);
        Assert.Equal(expectedPhrase, resolved);
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

