using OficinaMecanica.Execucao.Application.Messaging;
using OficinaMecanica.Execucao.Application.UseCases.Execucoes;

namespace OficinaMecanica.Execucao.InterfaceAdapters.Controllers;

public class IntegracaoCleanController
{
    private readonly ProcessarComandoEntradaUseCase _processar;
    public IntegracaoCleanController(ProcessarComandoEntradaUseCase processar) { _processar = processar; }
    public void Processar(MensagemIntegracao mensagem) => _processar.Executar(mensagem);
}
