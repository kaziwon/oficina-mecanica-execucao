using OficinaMecanica.Execucao.Application.Gateways;
using OficinaMecanica.Execucao.InterfaceAdapters.DataSources;

namespace OficinaMecanica.Execucao.InterfaceAdapters.Gateways;

public class TokenGateway : ITokenGateway
{
    private readonly ITokenSource _tokenSource;

    public TokenGateway(ITokenSource tokenSource)
    {
        _tokenSource = tokenSource;
    }

    public TokenGerado GerarToken(string username, string role)
    {
        return _tokenSource.GerarToken(username, role);
    }
}
