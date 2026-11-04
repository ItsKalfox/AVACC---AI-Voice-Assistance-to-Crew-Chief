using AVACC.AI;
using AVACC.Core.Configuration;
using AVACC.Core.Models;
using AVACC.Infrastructure;
using Xunit;

namespace AVACC.Core.Tests;

public class SettingsAndRegistryTests
{
    [Fact]
    public async Task SettingsService_SavesAndLoadsSettings_Correctly()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"avacc_settings_test_{Guid.NewGuid():N}.json");
        try
        {
            var service = new SettingsService(tempFile);
            var loaded = await service.LoadSettingsAsync();

            Assert.NotNull(loaded);
            Assert.Equal("Google Gemini", loaded.SimpleProvider);
            Assert.Equal("gemini-2.5-flash", loaded.SimpleModel);
            Assert.Equal("RightCtrl", loaded.PttKey);
            Assert.True(loaded.IsGlobalPttEnabled);

            // Modify and save
            loaded.PttKey = "Space";
            loaded.MaxRecordingDurationSeconds = 15;
            loaded.ArchitectureMode = ProcessingArchitectureMode.Advanced;
            await service.SaveSettingsAsync(loaded);

            // Reload in new instance
            var newService = new SettingsService(tempFile);
            var reloaded = await newService.LoadSettingsAsync();

            Assert.Equal("Space", reloaded.PttKey);
            Assert.Equal(15, reloaded.MaxRecordingDurationSeconds);
            Assert.Equal(ProcessingArchitectureMode.Advanced, reloaded.ArchitectureMode);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task CommandRegistry_LoadsDefaultCrewChiefCommands_Correctly()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"avacc_cmd_test_{Guid.NewGuid():N}.json");
        try
        {
            var service = new CommandRegistryService(tempFile);
            var commands = await service.LoadCommandsAsync();

            Assert.NotEmpty(commands);
            Assert.Contains(commands, c => c.CrewChiefCommandId == "HOW_MUCH_FUEL_TO_END_OF_RACE");
            Assert.Contains(commands, c => c.CrewChiefCommandId == "WHAT_ARE_MY_TYRE_PRESSURES");
            Assert.Contains(commands, c => c.CrewChiefCommandId == "WHAT_IS_MY_GAP_BEHIND");
            Assert.Contains(commands, c => c.CrewChiefCommandId == "FULL_UPDATE");
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task CommandRegistry_AddUpdateDelete_WorksCorrectly()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"avacc_cmd_test_{Guid.NewGuid():N}.json");
        try
        {
            var service = new CommandRegistryService(tempFile);
            await service.LoadCommandsAsync();

            var newCmd = new CommandDefinition
            {
                Name = "Test Custom Command",
                CrewChiefCommandId = "TEST_CUSTOM_COMMAND",
                Category = "Testing",
                Description = "Custom command for testing",
                IsEnabled = true,
                Examples = new List<string> { "Test request one", "Test request two" }
            };

            await service.AddCommandAsync(newCmd);
            var reloaded = service.Commands;
            Assert.Contains(reloaded, c => c.CrewChiefCommandId == "TEST_CUSTOM_COMMAND");

            // Update
            newCmd.Description = "Updated description";
            await service.UpdateCommandAsync(newCmd);
            var updated = service.Commands.First(c => c.CrewChiefCommandId == "TEST_CUSTOM_COMMAND");
            Assert.Equal("Updated description", updated.Description);

            // Delete
            await service.DeleteCommandAsync(newCmd.Id);
            var afterDelete = service.Commands;
            Assert.DoesNotContain(afterDelete, c => c.CrewChiefCommandId == "TEST_CUSTOM_COMMAND");
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task SecureKeyStorage_EncryptsAndDecrypts_WithDPAPI()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"avacc_keys_test_{Guid.NewGuid():N}");
        try
        {
            var storage = new SecureKeyStorage(tempDir);
            var provider = "Google Gemini";
            var apiKey = "AIzaSyD-TestKey1234567890ABCDEF";

            await storage.SetApiKeyAsync(provider, apiKey);
            Assert.True(await storage.HasApiKeyAsync(provider));

            var retrieved = await storage.GetApiKeyAsync(provider);
            Assert.Equal(apiKey, retrieved);

            // Verify the file on disk does NOT contain the plaintext API key
            var keyFiles = Directory.GetFiles(tempDir);
            Assert.Single(keyFiles);
            var rawBytes = File.ReadAllBytes(keyFiles[0]);
            var rawText = System.Text.Encoding.UTF8.GetString(rawBytes);
            Assert.DoesNotContain(apiKey, rawText); // Never plaintext!

            // Remove
            await storage.RemoveApiKeyAsync(provider);
            Assert.False(await storage.HasApiKeyAsync(provider));
            Assert.Null(await storage.GetApiKeyAsync(provider));
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void PromptBuilder_ParsesValidJsonResponse_AndMatchesCommand()
    {
        var commands = CommandRegistryService.GetDefaultCommands();
        var jsonResponse = """
        {
            "intent": "fuel_to_end",
            "command_id": "HOW_MUCH_FUEL_TO_END_OF_RACE",
            "confidence": 0.98,
            "reason": "Driver asked if they can make it to the end with current fuel.",
            "requires_clarification": false
        }
        """;

        var result = PromptBuilder.ParseStructuredResponse(jsonResponse, "Can we make it to the finish with current fuel?", commands);

        Assert.Equal("HOW_MUCH_FUEL_TO_END_OF_RACE", result.CommandId);
        Assert.Equal(0.98, result.Confidence);
        Assert.False(result.RequiresClarification);
        Assert.Null(result.Error);
    }

    [Fact]
    public void PromptBuilder_RejectsArbitraryHallucinatedCommandId()
    {
        var commands = CommandRegistryService.GetDefaultCommands();
        var jsonResponse = """
        {
            "intent": "random_request",
            "command_id": "HALLUCINATED_NON_EXISTENT_ID",
            "confidence": 0.90,
            "reason": "Arbitrary command ID invented by model.",
            "requires_clarification": false
        }
        """;

        var result = PromptBuilder.ParseStructuredResponse(jsonResponse, "Some completely unknown phrase", commands);

        // Must reject unknown command ID
        Assert.Null(result.CommandId);
        Assert.True(result.RequiresClarification);
    }

    [Fact]
    public void PromptBuilder_RejectsDisabledCommand()
    {
        var commands = CommandRegistryService.GetDefaultCommands();
        // Disable the fuel command
        var fuelCmd = commands.First(c => c.CrewChiefCommandId == "HOW_MUCH_FUEL_TO_END_OF_RACE");
        fuelCmd.IsEnabled = false;

        var jsonResponse = """
        {
            "intent": "fuel_to_end",
            "command_id": "HOW_MUCH_FUEL_TO_END_OF_RACE",
            "confidence": 0.95,
            "reason": "Fuel check",
            "requires_clarification": false
        }
        """;

        var result = PromptBuilder.ParseStructuredResponse(jsonResponse, "How much fuel do we need?", commands);

        // Disabled command must not be matched
        Assert.Null(result.CommandId);
        Assert.True(result.RequiresClarification);
    }
}

