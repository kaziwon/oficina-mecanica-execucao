using OficinaMecanica.Execucao.Application.Messaging;

namespace OficinaMecanica.Execucao.InterfaceAdapters.DataSources;

public interface IOutboxDataSource
{
    List<MensagemIntegracao> ObterPendentes(int limite);
    void ConfirmarPublicacao(Guid messageId);
}
