using OficinaMecanica.Execucao.Application.Gateways;
using OficinaMecanica.Execucao.Domain.Entities;
using OficinaMecanica.Execucao.InterfaceAdapters.DataSources;

namespace OficinaMecanica.Execucao.InterfaceAdapters.Gateways;

public class ExecucaoGateway : IExecucaoGateway
{
    private readonly IExecucaoDataSource _source;
    public ExecucaoGateway(IExecucaoDataSource source) { _source = source; }
    public ExecucaoServico Adicionar(ExecucaoServico execucao) => _source.Adicionar(execucao);
    public ExecucaoServico? ObterPorId(Guid id) => _source.ObterPorId(id);
    public ExecucaoServico? ObterPorOrdemServicoId(Guid id) => _source.ObterPorOrdemServicoId(id);
    public List<ExecucaoServico> ObterFila(StatusExecucao? status) => _source.ObterFila(status);
    public bool Atualizar(ExecucaoServico execucao, long versaoEsperada) => _source.Atualizar(execucao, versaoEsperada);
}
