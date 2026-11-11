using OficinaMecanica.Execucao.Application.Exceptions;
using OficinaMecanica.Execucao.Application.Gateways;
using OficinaMecanica.Execucao.Application.Mappers.Execucoes;
using OficinaMecanica.Execucao.Application.Outputs.Execucoes;

namespace OficinaMecanica.Execucao.Application.UseCases.Execucoes;

public class ConcluirReparoUseCase
{
    private readonly IExecucaoGateway _execucoes;
    private readonly IConclusaoReparoGateway _conclusao;
    public ConcluirReparoUseCase(IExecucaoGateway execucoes, IConclusaoReparoGateway conclusao)
    { _execucoes = execucoes; _conclusao = conclusao; }

    public ExecucaoOutput? Executar(Guid id)
    {
        for (var tentativa = 0; tentativa < 5; tentativa++)
        {
            var execucao = _execucoes.ObterPorId(id);
            if (execucao is null) return null;
            var versao = execucao.Versao;
            if (!execucao.ConcluirReparo()) return ExecucaoOutputMapper.Mapear(execucao);
            if (_conclusao.ConsumirEConcluir(execucao, versao))
            {
                var salva = _execucoes.ObterPorId(id)
                    ?? throw new ConflitoException("Conclusao salva; consulte a execucao novamente.");
                return ExecucaoOutputMapper.Mapear(salva);
            }
        }
        throw new ConflitoException("A execucao foi alterada durante a conclusao do reparo. Consulte novamente.");
    }
}
