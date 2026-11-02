namespace OficinaMecanica.Execucao.Application.Gateways;

public interface ITokenGateway
{
    TokenGerado GerarToken(string username, string role);
}
