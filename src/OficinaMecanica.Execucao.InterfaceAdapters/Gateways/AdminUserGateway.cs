using OficinaMecanica.Execucao.Application.Gateways;
using OficinaMecanica.Execucao.InterfaceAdapters.DataSources;

namespace OficinaMecanica.Execucao.InterfaceAdapters.Gateways;

public class AdminUserGateway : IAdminUserGateway
{
    private readonly IAdminUserSource _adminUserSource;

    public AdminUserGateway(IAdminUserSource adminUserSource)
    {
        _adminUserSource = adminUserSource;
    }

    public bool CredenciaisValidas(string username, string password)
    {
        return _adminUserSource.CredenciaisValidas(username, password);
    }
}
