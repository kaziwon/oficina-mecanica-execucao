using Microsoft.Extensions.Options;
using OficinaMecanica.Execucao.Infrastructure.Settings;
using OficinaMecanica.Execucao.InterfaceAdapters.DataSources;

namespace OficinaMecanica.Execucao.Infrastructure.Sources;

public class AdminUserSettingsSource : IAdminUserSource
{
    private readonly AdminUserSettings _adminUserSettings;

    public AdminUserSettingsSource(IOptions<AdminUserSettings> adminUserSettings)
    {
        _adminUserSettings = adminUserSettings.Value;
    }

    public bool CredenciaisValidas(string username, string password)
    {
        return username == _adminUserSettings.Username && password == _adminUserSettings.Password;
    }
}
