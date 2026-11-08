using OficinaMecanica.Execucao.Application.Gateways;
using OficinaMecanica.Execucao.Application.Messaging;
using OficinaMecanica.Execucao.InterfaceAdapters.DataSources;

namespace OficinaMecanica.Execucao.InterfaceAdapters.Gateways;

public class OutboxGateway : IOutboxGateway
{
    private readonly IOutboxDataSource _source;
    public OutboxGateway(IOutboxDataSource source) { _source = source; }
    public List<MensagemIntegracao> ObterPendentes(int limite) => _source.ObterPendentes(limite);
    public void ConfirmarPublicacao(Guid messageId) => _source.ConfirmarPublicacao(messageId);
}
