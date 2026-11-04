using System.Text;
using System.Text.Json;
using AVACC.Core.Models;

namespace AVACC.AI;

public static class PromptBuilder
{
    public static string BuildSystemPrompt(IReadOnlyList<CommandDefinition> availableCommands)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are AVACC, an AI voice assistant for Crew Chief in sim-racing (Assetto Corsa Competizione).");
        sb.AppendLine("Your job is to listen to the driver's spoken voice transcript and identify which Crew Chief command they intended.");
        sb.AppendLine();
        sb.AppendLine("STRICT RULES:");
        sb.AppendLine("1. Choose ONLY from the available Command IDs listed below. NEVER invent or hallucinate a Command ID.");
        sb.AppendLine("2. If the user's request does not match any available command with reasonable confidence, set command_id to null and requires_clarification to true.");
        sb.AppendLine("3. Self-report your confidence (0.0 to 1.0).");
        sb.AppendLine("4. Respond ONLY with a valid JSON object matching the requested schema. No conversational filler.");
        sb.AppendLine();
        sb.AppendLine("OUTPUT JSON SCHEMA:");
        sb.AppendLine("{");
        sb.AppendLine("  \"transcript\": \"Exact transcription of what the driver spoke in the audio\",");
        sb.AppendLine("  \"intent\": \"short_snake_case_intent_description\",");
        sb.AppendLine("  \"command_id\": \"EXACT_CREW_CHIEF_COMMAND_ID_OR_NULL\",");
        sb.AppendLine("  \"confidence\": 0.95,");
        sb.AppendLine("  \"reason\": \"Concise reason for mapping\",");
        sb.AppendLine("  \"requires_clarification\": false");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("AVAILABLE COMMAND REGISTRY:");

        foreach (var cmd in availableCommands.Where(c => c.IsEnabled))
        {
            sb.AppendLine($"- ID: {cmd.CrewChiefCommandId}");
            sb.AppendLine($"  Name: {cmd.Name}");
            sb.AppendLine($"  Category: {cmd.Category}");
            sb.AppendLine($"  Description: {cmd.Description}");
            if (cmd.Examples.Count > 0)
            {
                sb.AppendLine($"  Examples: {string.Join(" | ", cmd.Examples)}");
            }
            if (cmd.Aliases.Count > 0)
            {
                sb.AppendLine($"  Aliases: {string.Join(", ", cmd.Aliases)}");
            }
        }

        return sb.ToString();
    }

    public static string BuildUserPrompt(string transcript)
    {
        return $"Driver Voice Request: \"{transcript.Trim()}\"\nIdentify the matching Crew Chief command:";
    }

    public static VoiceCommandResult ParseStructuredResponse(string jsonResponse, string originalTranscript, IReadOnlyList<CommandDefinition> availableCommands)
    {
        var result = new VoiceCommandResult
        {
            Transcript = originalTranscript
        };

        if (string.IsNullOrWhiteSpace(jsonResponse))
        {
            result.Error = "Empty response received from AI model.";
            return result;
        }

        try
        {
            // Extract JSON from potential code fences
            var cleanedJson = jsonResponse.Trim();
            if (cleanedJson.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
            {
                cleanedJson = cleanedJson.Substring(7);
            }
            else if (cleanedJson.StartsWith("```"))
            {
                cleanedJson = cleanedJson.Substring(3);
            }

            if (cleanedJson.EndsWith("```"))
            {
                cleanedJson = cleanedJson.Substring(0, cleanedJson.Length - 3);
            }

            cleanedJson = cleanedJson.Trim();

            using var doc = JsonDocument.Parse(cleanedJson);
            var root = doc.RootElement;

            if (root.TryGetProperty("transcript", out var trEl) && trEl.ValueKind == JsonValueKind.String)
            {
                var tr = trEl.GetString()?.Trim();
                if (!string.IsNullOrEmpty(tr))
                {
                    result.Transcript = tr;
                }
            }

            result.Intent = root.TryGetProperty("intent", out var intentEl) ? intentEl.GetString() ?? "" : "";
            result.Reason = root.TryGetProperty("reason", out var reasonEl) ? reasonEl.GetString() : null;
            result.RequiresClarification = root.TryGetProperty("requires_clarification", out var reqEl) && reqEl.GetBoolean();

            if (root.TryGetProperty("confidence", out var confEl) && confEl.TryGetDouble(out var confVal))
            {
                result.Confidence = Math.Clamp(confVal, 0.0, 1.0);
            }

            string? reportedCommandId = null;
            if (root.TryGetProperty("command_id", out var cmdEl) && cmdEl.ValueKind == JsonValueKind.String)
            {
                reportedCommandId = cmdEl.GetString()?.Trim();
            }

            // Validation: Reject unknown or disabled command IDs
            if (!string.IsNullOrEmpty(reportedCommandId))
            {
                var matchingCommand = availableCommands.FirstOrDefault(c =>
                    string.Equals(c.CrewChiefCommandId, reportedCommandId, StringComparison.OrdinalIgnoreCase) && c.IsEnabled);

                if (matchingCommand != null)
                {
                    result.CommandId = matchingCommand.CrewChiefCommandId;
                }
                else
                {
                    // Invalid/unknown ID generated by model
                    result.CommandId = null;
                    result.RequiresClarification = true;
                    result.Reason = $"Model returned unrecognized or disabled command ID '{reportedCommandId}'. Ignored.";
                }
            }

            return result;
        }
        catch (JsonException ex)
        {
            result.Error = $"Failed to parse model JSON: {ex.Message}";
            result.RequiresClarification = true;
            return result;
        }
    }
}

