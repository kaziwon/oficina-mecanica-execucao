using OficinaMecanica.Execucao.Application.Exceptions;
using OficinaMecanica.Execucao.Application.Gateways;
using OficinaMecanica.Execucao.Application.Mappers.Execucoes;
using OficinaMecanica.Execucao.Application.Outputs.Execucoes;

namespace OficinaMecanica.Execucao.Application.UseCases.Execucoes;

public class IniciarDiagnosticoUseCase
{
    private readonly IExecucaoGateway _execucoes;
    public IniciarDiagnosticoUseCase(IExecucaoGateway execucoes) { _execucoes = execucoes; }
    public ExecucaoOutput? Executar(Guid id)
    {
        var execucao = _execucoes.ObterPorId(id);
        if (execucao is null) return null;
        var versao = execucao.Versao;
        execucao.IniciarDiagnostico();
        if (!_execucoes.Atualizar(execucao, versao))
            throw new ConflitoException("A execucao foi alterada por outra requisicao. Consulte novamente.");
        return ExecucaoOutputMapper.Mapear(execucao);
    }
}
