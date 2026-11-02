using OficinaMecanica.Execucao.Domain.Entities;

namespace OficinaMecanica.Execucao.InterfaceAdapters.DataSources;

public interface IExecucaoDataSource
{
    ExecucaoServico Adicionar(ExecucaoServico execucao);
    ExecucaoServico? ObterPorId(Guid id);
    ExecucaoServico? ObterPorOrdemServicoId(Guid ordemServicoId);
    List<ExecucaoServico> ObterFila(StatusExecucao? status);
    bool Atualizar(ExecucaoServico execucao, long versaoEsperada);
}
