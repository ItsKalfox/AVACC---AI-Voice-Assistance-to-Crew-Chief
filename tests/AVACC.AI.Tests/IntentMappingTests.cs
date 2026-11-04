using AVACC.AI;
using AVACC.Infrastructure;
using Xunit;

namespace AVACC.AI.Tests;

public class IntentMappingTests
{
    [Theory]
    [InlineData("How much fuel do we need?", "HOW_MUCH_FUEL_TO_END_OF_RACE")]
    [InlineData("Can we make it to the finish?", "HOW_MUCH_FUEL_TO_END_OF_RACE")]
    [InlineData("What's our fuel requirement for the next stint?", "HOW_MUCH_FUEL_TO_END_OF_RACE")]
    [InlineData("How is my fuel?", "HOWS_MY_FUEL")]
    [InlineData("What are my tyre pressures?", "WHAT_ARE_MY_TYRE_PRESSURES")]
    [InlineData("How are the tyres looking?", "HOW_ARE_MY_TYRES")]
    [InlineData("What is my gap behind?", "WHAT_IS_MY_GAP_BEHIND")]
    [InlineData("What is my gap ahead?", "WHAT_IS_MY_GAP_AHEAD")]
    [InlineData("Give me a quick race update", "FULL_UPDATE")]
    [InlineData("What was my last lap time?", "WHATS_MY_LAST_LAP_TIME")]
    [InlineData("Box this lap", "BOX_THIS_LAP")]
    [InlineData("Radio check", "RADIO_CHECK")]
    public void PromptBuilder_MapsKnownCommands_WhenReturnedInModelJson(string naturalLanguagePhrase, string expectedCommandId)
    {
        var commands = CommandRegistryService.GetDefaultCommands();
        var simulatedJson = $$"""
        {
            "intent": "matched_intent",
            "command_id": "{{expectedCommandId}}",
            "confidence": 0.97,
            "reason": "Driver requested: {{naturalLanguagePhrase}}",
            "requires_clarification": false
        }
        """;

        var result = PromptBuilder.ParseStructuredResponse(simulatedJson, naturalLanguagePhrase, commands);

        Assert.Equal(expectedCommandId, result.CommandId);
        Assert.NotNull(result.Confidence);
        Assert.True(result.Confidence >= 0.90);
        Assert.False(result.RequiresClarification);
    }

    [Fact]
    public void PromptBuilder_HandlesEmptyOrWhitespaceResponse_Gracefully()
    {
        var commands = CommandRegistryService.GetDefaultCommands();
        var result = PromptBuilder.ParseStructuredResponse("", "test", commands);

        Assert.NotNull(result.Error);
        Assert.Null(result.CommandId);
    }

    [Fact]
    public void PromptBuilder_HandlesMarkdownCodeFences_InJsonResponse()
    {
        var commands = CommandRegistryService.GetDefaultCommands();
        var markdownJson = """
        ```json
        {
            "intent": "tyre_pressures",
            "command_id": "WHAT_ARE_MY_TYRE_PRESSURES",
            "confidence": 0.94,
            "reason": "Tyre pressures query",
            "requires_clarification": false
        }
        ```
        """;

        var result = PromptBuilder.ParseStructuredResponse(markdownJson, "Tyre pressures please", commands);

        Assert.Equal("WHAT_ARE_MY_TYRE_PRESSURES", result.CommandId);
        Assert.Equal(0.94, result.Confidence);
        Assert.False(result.RequiresClarification);
    }

    [Fact]
    public void AIProviderFactory_ResolvesCorrectProvider_BasedOnSettings()
    {
        var keyStorage = new SecureKeyStorage();
        var gemini = new AVACC.AI.Providers.GeminiProvider(keyStorage);
        var openai = new AVACC.AI.Providers.OpenAIProvider(keyStorage);
        var groq = new AVACC.AI.Providers.GroqProvider(keyStorage);

        var factory = new AIProviderFactory(gemini, openai, groq);

        Assert.Equal("Groq", factory.GetInterpreter("Groq").ProviderName);
        Assert.Equal("Groq", factory.GetMultimodalProvider("Groq").ProviderName);
        Assert.Equal("Groq", factory.GetSttProvider("Groq").ProviderName);

        Assert.Equal("Google Gemini", factory.GetInterpreter("Google Gemini").ProviderName);
        Assert.Equal("Google Gemini", factory.GetMultimodalProvider("Google Gemini").ProviderName);

        Assert.Equal("OpenAI", factory.GetInterpreter("OpenAI").ProviderName);
        Assert.Equal("OpenAI", factory.GetMultimodalProvider("OpenAI").ProviderName);
    }
}

