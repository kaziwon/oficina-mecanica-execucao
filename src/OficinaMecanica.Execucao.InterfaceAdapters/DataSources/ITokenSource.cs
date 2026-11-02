using OficinaMecanica.Execucao.Application.Gateways;

namespace OficinaMecanica.Execucao.InterfaceAdapters.DataSources;

public interface ITokenSource
{
    TokenGerado GerarToken(string username, string role);
}
