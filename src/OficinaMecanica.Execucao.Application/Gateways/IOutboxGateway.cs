using OficinaMecanica.Execucao.Application.Messaging;

namespace OficinaMecanica.Execucao.Application.Gateways;

public interface IOutboxGateway
{
    List<MensagemIntegracao> ObterPendentes(int limite);
    void ConfirmarPublicacao(Guid messageId);
}
