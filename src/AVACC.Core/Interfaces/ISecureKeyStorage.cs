namespace AVACC.Core.Interfaces;

public interface ISecureKeyStorage
{
    Task<string?> GetApiKeyAsync(string providerName);
    Task SetApiKeyAsync(string providerName, string apiKey);
    Task RemoveApiKeyAsync(string providerName);
    Task<bool> HasApiKeyAsync(string providerName);
}

