using OficinaMecanica.Execucao.Domain.Entities;

namespace OficinaMecanica.Execucao.Application.Gateways;

public interface IExecucaoGateway
{
    ExecucaoServico Adicionar(ExecucaoServico execucao);
    ExecucaoServico? ObterPorId(Guid id);
    ExecucaoServico? ObterPorOrdemServicoId(Guid ordemServicoId);
    List<ExecucaoServico> ObterFila(StatusExecucao? status);
    bool Atualizar(ExecucaoServico execucao, long versaoEsperada);
}
