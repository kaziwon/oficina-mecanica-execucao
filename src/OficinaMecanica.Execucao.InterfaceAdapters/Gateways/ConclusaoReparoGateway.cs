using OficinaMecanica.Execucao.Application.Gateways;
using OficinaMecanica.Execucao.Domain.Entities;
using OficinaMecanica.Execucao.InterfaceAdapters.DataSources;

namespace OficinaMecanica.Execucao.InterfaceAdapters.Gateways;

public class ConclusaoReparoGateway : IConclusaoReparoGateway
{
    private readonly IConclusaoReparoDataSource _source;
    public ConclusaoReparoGateway(IConclusaoReparoDataSource source) { _source = source; }
    public bool ConsumirEConcluir(ExecucaoServico execucao, long versaoEsperada)
        => _source.ConsumirEConcluir(execucao, versaoEsperada);
}
