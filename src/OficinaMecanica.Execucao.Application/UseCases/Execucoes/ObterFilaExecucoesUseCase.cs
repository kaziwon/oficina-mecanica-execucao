using OficinaMecanica.Execucao.Application.Exceptions;
using OficinaMecanica.Execucao.Application.Gateways;
using OficinaMecanica.Execucao.Application.Mappers.Execucoes;
using OficinaMecanica.Execucao.Application.Outputs.Execucoes;
using OficinaMecanica.Execucao.Domain.Entities;

namespace OficinaMecanica.Execucao.Application.UseCases.Execucoes;

public class ObterFilaExecucoesUseCase
{
    private readonly IExecucaoGateway _execucoes;
    public ObterFilaExecucoesUseCase(IExecucaoGateway execucoes) { _execucoes = execucoes; }
    public List<ExecucaoOutput> Executar(StatusExecucao? status)
    {
        if (status.HasValue && !Enum.IsDefined(status.Value))
            throw new ValidacaoException("Status de execucao invalido.");
        return _execucoes.ObterFila(status).Select(ExecucaoOutputMapper.Mapear).ToList();
    }
}
