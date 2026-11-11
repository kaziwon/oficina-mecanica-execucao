using OficinaMecanica.Execucao.Application.Messaging;
using OficinaMecanica.Execucao.Application.UseCases.Execucoes;

namespace OficinaMecanica.Execucao.InterfaceAdapters.Controllers;

public class IntegracaoCleanController
{
    private readonly ProcessarComandoEntradaUseCase _processar;
    private readonly ProcessarRecusaOrcamentoUseCase _recusa;
    public IntegracaoCleanController(ProcessarComandoEntradaUseCase processar, ProcessarRecusaOrcamentoUseCase recusa)
    { _processar = processar; _recusa = recusa; }
    public void Processar(MensagemIntegracao mensagem)
    {
        if (mensagem.Type == "CancelarExecucaoPorRecusaOrcamento") _recusa.Executar(mensagem);
        else _processar.Executar(mensagem);
    }
}
