using OficinaMecanica.Execucao.Application.Gateways;
using OficinaMecanica.Execucao.Domain.Entities;
using OficinaMecanica.Execucao.InterfaceAdapters.DataSources;

namespace OficinaMecanica.Execucao.InterfaceAdapters.Gateways;

public class EntradaExecucaoGateway : IEntradaExecucaoGateway
{
    private readonly IEntradaExecucaoDataSource _source;
    public EntradaExecucaoGateway(IEntradaExecucaoDataSource source) { _source = source; }
    public ExecucaoServico? ObterPorOrdemServicoId(Guid id) => _source.ObterPorOrdemServicoId(id);
    public bool TentarInserir(ExecucaoServico execucao) => _source.TentarInserir(execucao);
    public bool TentarAtualizar(ExecucaoServico execucao, long versaoEsperada) => _source.TentarAtualizar(execucao, versaoEsperada);
}
