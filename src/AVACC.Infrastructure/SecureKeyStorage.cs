using System.IO;
using System.Security.Cryptography;
using System.Text;
using AVACC.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace AVACC.Infrastructure;

/// <summary>
/// Securely stores API keys on Windows using Windows Data Protection API (DPAPI).
/// Keys are encrypted using the current user's Windows credentials and never stored as plaintext.
/// </summary>
public class SecureKeyStorage : ISecureKeyStorage
{
    private readonly string _storageDirectory;
    private readonly ILogger<SecureKeyStorage>? _logger;
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("AVACC.DPAPI.KeyProtection.Entropy.v1");

    public SecureKeyStorage(string? customDirectory = null, ILogger<SecureKeyStorage>? logger = null)
    {
        _logger = logger;
        _storageDirectory = customDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AVACC",
            "keys");

        if (!Directory.Exists(_storageDirectory))
        {
            Directory.CreateDirectory(_storageDirectory);
        }
    }

    private string GetKeyFilePath(string providerName)
    {
        var sanitized = Path.GetInvalidFileNameChars()
            .Aggregate(providerName.ToLowerInvariant().Trim(), (current, c) => current.Replace(c, '_'));
        return Path.Combine(_storageDirectory, $"{sanitized}.dat");
    }

    public Task<string?> GetApiKeyAsync(string providerName)
    {
        try
        {
            var filePath = GetKeyFilePath(providerName);
            if (!File.Exists(filePath))
            {
                return Task.FromResult<string?>(null);
            }

            var encryptedBytes = File.ReadAllBytes(filePath);
            var decryptedBytes = ProtectedData.Unprotect(encryptedBytes, Entropy, DataProtectionScope.CurrentUser);
            var apiKey = Encoding.UTF8.GetString(decryptedBytes);
            return Task.FromResult<string?>(apiKey);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to decrypt API key for provider {Provider}", providerName);
            return Task.FromResult<string?>(null);
        }
    }

    public Task SetApiKeyAsync(string providerName, string apiKey)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return RemoveApiKeyAsync(providerName);
            }

            var filePath = GetKeyFilePath(providerName);
            var rawBytes = Encoding.UTF8.GetBytes(apiKey.Trim());
            var encryptedBytes = ProtectedData.Protect(rawBytes, Entropy, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(filePath, encryptedBytes);
            _logger?.LogInformation("Securely saved DPAPI encrypted API key for provider {Provider}", providerName);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to securely save API key for provider {Provider}", providerName);
            throw;
        }
    }

    public Task RemoveApiKeyAsync(string providerName)
    {
        try
        {
            var filePath = GetKeyFilePath(providerName);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger?.LogInformation("Removed stored API key for provider {Provider}", providerName);
            }
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to remove API key for provider {Provider}", providerName);
            throw;
        }
    }

    public Task<bool> HasApiKeyAsync(string providerName)
    {
        var filePath = GetKeyFilePath(providerName);
        return Task.FromResult(File.Exists(filePath) && new FileInfo(filePath).Length > 0);
    }
}
