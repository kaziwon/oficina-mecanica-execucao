using OficinaMecanica.Execucao.Application.Exceptions;
using OficinaMecanica.Execucao.Application.Gateways;
using OficinaMecanica.Execucao.Application.Mappers.Execucoes;
using OficinaMecanica.Execucao.Application.Outputs.Execucoes;

namespace OficinaMecanica.Execucao.Application.UseCases.Execucoes;

public class IniciarReparoUseCase
{
    private readonly IExecucaoGateway _execucoes;
    private readonly IInicioReparoGateway _inicio;

    public IniciarReparoUseCase(IExecucaoGateway execucoes, IInicioReparoGateway inicio)
    {
        _execucoes = execucoes;
        _inicio = inicio;
    }

    public ExecucaoOutput? Executar(Guid id)
    {
        for (var tentativa = 0; tentativa < 5; tentativa++)
        {
            var execucao = _execucoes.ObterPorId(id);
            if (execucao is null) return null;
            var versao = execucao.Versao;
            if (!execucao.IniciarReparo())
                return ExecucaoOutputMapper.Mapear(execucao);
            if (_inicio.ReservarEIniciar(execucao, versao))
            {
                var salva = _execucoes.ObterPorId(id)
                    ?? throw new ConflitoException("Inicio salvo; consulte a execucao novamente.");
                return ExecucaoOutputMapper.Mapear(salva);
            }
        }
        throw new ConflitoException("A execucao foi alterada durante o inicio do reparo. Consulte novamente.");
    }
}
