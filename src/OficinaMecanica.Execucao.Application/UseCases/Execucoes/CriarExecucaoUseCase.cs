using OficinaMecanica.Execucao.Application.Exceptions;
using OficinaMecanica.Execucao.Application.Gateways;
using OficinaMecanica.Execucao.Application.Inputs.Execucoes;
using OficinaMecanica.Execucao.Application.Mappers.Execucoes;
using OficinaMecanica.Execucao.Application.Outputs.Execucoes;
using OficinaMecanica.Execucao.Domain.Entities;

namespace OficinaMecanica.Execucao.Application.UseCases.Execucoes;

public class CriarExecucaoUseCase
{
    private readonly IExecucaoGateway _execucoes;
    public CriarExecucaoUseCase(IExecucaoGateway execucoes) { _execucoes = execucoes; }

    public ExecucaoOutput Executar(CriarExecucaoInput input)
    {
        if (input.OrdemServicoId == Guid.Empty)
            throw new ValidacaoException("Informe o ID da OS.");
        if (string.IsNullOrWhiteSpace(input.DescricaoSolicitacao) || input.DescricaoSolicitacao.Trim().Length > 1000)
            throw new ValidacaoException("Informe a solicitacao com ate 1000 caracteres.");
        if (_execucoes.ObterPorOrdemServicoId(input.OrdemServicoId) is not null)
            throw new ConflitoException("Ja existe uma execucao vinculada a esta OS.");
        var agora = DateTime.UtcNow;
        var execucao = new ExecucaoServico
        {
            Id = Guid.NewGuid(),
            OrdemServicoId = input.OrdemServicoId,
            DescricaoSolicitacao = input.DescricaoSolicitacao.Trim(),
            Status = StatusExecucao.AguardandoDiagnostico,
            CriadaEm = agora,
            Historico = [new HistoricoExecucao { Status = StatusExecucao.AguardandoDiagnostico, RegistradoEm = agora }]
        };
        return ExecucaoOutputMapper.Mapear(_execucoes.Adicionar(execucao));
    }
}
