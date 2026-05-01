namespace mattias_tonna_swd63a_pftc.interfaces;

public interface IGoogleSecretManagerService
{
    Task<string> GetSecretAsync(string secretName);
    Task LoadSecretsIntoConfigurationAsync(IConfiguration config);
}