using AVACC.AI.Providers;
using AVACC.Core.Interfaces;

namespace AVACC.AI;

public class AIProviderFactory : IAIProviderFactory
{
    private readonly GeminiProvider _gemini;
    private readonly OpenAIProvider _openAi;
    private readonly GroqProvider _groq;

    public AIProviderFactory(GeminiProvider gemini, OpenAIProvider openAi, GroqProvider groq)
    {
        _gemini = gemini;
        _openAi = openAi;
        _groq = groq;
    }

    public IMultimodalVoiceProvider GetMultimodalProvider(string providerName)
    {
        return providerName?.Trim().ToLowerInvariant() switch
        {
            "groq" => _groq,
            "openai" => _openAi,
            _ => _gemini
        };
    }

    public ISpeechToTextProvider GetSttProvider(string providerName)
    {
        return providerName?.Trim().ToLowerInvariant() switch
        {
            "groq" => _groq,
            "openai" => _openAi,
            _ => _gemini
        };
    }

    public ICommandInterpreter GetInterpreter(string providerName)
    {
        return providerName?.Trim().ToLowerInvariant() switch
        {
            "groq" => _groq,
            "openai" => _openAi,
            _ => _gemini
        };
    }
}

