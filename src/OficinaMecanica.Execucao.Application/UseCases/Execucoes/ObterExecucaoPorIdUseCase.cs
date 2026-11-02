using OficinaMecanica.Execucao.Application.Gateways;
using OficinaMecanica.Execucao.Application.Mappers.Execucoes;
using OficinaMecanica.Execucao.Application.Outputs.Execucoes;

namespace OficinaMecanica.Execucao.Application.UseCases.Execucoes;

public class ObterExecucaoPorIdUseCase
{
    private readonly IExecucaoGateway _execucoes;
    public ObterExecucaoPorIdUseCase(IExecucaoGateway execucoes) { _execucoes = execucoes; }
    public ExecucaoOutput? Executar(Guid id)
    {
        var execucao = _execucoes.ObterPorId(id);
        return execucao is null ? null : ExecucaoOutputMapper.Mapear(execucao);
    }
}
