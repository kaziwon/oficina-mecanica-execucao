using OficinaMecanica.Execucao.Application.Messaging;
using OficinaMecanica.Execucao.Application.UseCases.Execucoes;

namespace OficinaMecanica.Execucao.InterfaceAdapters.Controllers;

public class IntegracaoCleanController
{
    private readonly ProcessarComandoEntradaUseCase _processar;
    private readonly ProcessarRecusaOrcamentoUseCase _recusa;
    private readonly ProcessarLiberacaoExecucaoUseCase _liberacao;
    private readonly ProcessarCancelamentoPagoUseCase _cancelamentoPago;
    public IntegracaoCleanController(ProcessarComandoEntradaUseCase processar, ProcessarRecusaOrcamentoUseCase recusa,
        ProcessarLiberacaoExecucaoUseCase liberacao, ProcessarCancelamentoPagoUseCase cancelamentoPago)
    { _processar = processar; _recusa = recusa; _liberacao = liberacao; _cancelamentoPago = cancelamentoPago; }
    public void Processar(MensagemIntegracao mensagem)
    {
        if (mensagem.Type == "CancelarExecucaoPorRecusaOrcamento") _recusa.Executar(mensagem);
        else if (mensagem.Type == "LiberarExecucao") _liberacao.Executar(mensagem);
        else if (mensagem.Type == "CancelarReparoPago") _cancelamentoPago.Executar(mensagem);
        else _processar.Executar(mensagem);
    }
}
