using OficinaMecanica.Execucao.Domain.Entities;

namespace OficinaMecanica.Execucao.InterfaceAdapters.DataSources;

public interface IEntradaExecucaoDataSource
{
    ExecucaoServico? ObterPorOrdemServicoId(Guid id);
    bool TentarInserir(ExecucaoServico execucao);
    bool TentarAtualizar(ExecucaoServico execucao, long versaoEsperada);
}
