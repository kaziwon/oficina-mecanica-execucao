using OficinaMecanica.Execucao.Application.Gateways;
using OficinaMecanica.Execucao.Domain.Entities;
using OficinaMecanica.Execucao.InterfaceAdapters.DataSources;

namespace OficinaMecanica.Execucao.InterfaceAdapters.Gateways;

public class InicioReparoGateway : IInicioReparoGateway
{
    private readonly IInicioReparoDataSource _source;
    public InicioReparoGateway(IInicioReparoDataSource source) { _source = source; }
    public bool ReservarEIniciar(ExecucaoServico execucao, long versaoEsperada)
        => _source.ReservarEIniciar(execucao, versaoEsperada);
}
