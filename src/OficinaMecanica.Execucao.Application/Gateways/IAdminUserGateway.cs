namespace OficinaMecanica.Execucao.Application.Gateways;

public interface IAdminUserGateway
{
    bool CredenciaisValidas(string username, string password);
}
