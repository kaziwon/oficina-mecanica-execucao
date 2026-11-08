using OficinaMecanica.Execucao.Domain.Entities;

namespace OficinaMecanica.Execucao.Application.Gateways;

public interface IEntradaExecucaoGateway
{
    ExecucaoServico? ObterPorOrdemServicoId(Guid id);
    bool TentarInserir(ExecucaoServico execucao);
    bool TentarAtualizar(ExecucaoServico execucao, long versaoEsperada);
}
