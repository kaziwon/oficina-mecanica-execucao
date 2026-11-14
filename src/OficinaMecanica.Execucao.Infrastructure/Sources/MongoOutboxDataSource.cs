using MongoDB.Driver;
using OficinaMecanica.Execucao.Application.Mappers.Execucoes;
using OficinaMecanica.Execucao.Application.Messaging;
using OficinaMecanica.Execucao.Domain.Entities;
using OficinaMecanica.Execucao.Infrastructure.Persistence;
using OficinaMecanica.Execucao.InterfaceAdapters.DataSources;

namespace OficinaMecanica.Execucao.Infrastructure.Sources;

public class MongoOutboxDataSource : IOutboxDataSource
{
    private readonly IMongoCollection<ExecucaoServico> _execucoes;
    public MongoOutboxDataSource(MongoDbContext database) { _execucoes = database.Execucoes; }

    public List<MensagemIntegracao> ObterPendentes(int limite)
    {
        var filter = Builders<ExecucaoServico>.Filter.ElemMatch(item => item.RespostasEntrada, item => item.PublicadaEm == null)
            | Builders<ExecucaoServico>.Filter.ElemMatch(item => item.EventosDiagnostico, item => item.PublicadoEm == null)
            | Builders<ExecucaoServico>.Filter.ElemMatch(item => item.RespostasRecusaOrcamento, item => item.PublicadaEm == null)
            | Builders<ExecucaoServico>.Filter.ElemMatch(item => item.RespostasLiberacao, item => item.PublicadaEm == null)
            | Builders<ExecucaoServico>.Filter.ElemMatch(item => item.EventosReparo, item => item.PublicadoEm == null)
            | Builders<ExecucaoServico>.Filter.ElemMatch(item => item.EventosConclusaoReparo, item => item.PublicadoEm == null)
            | Builders<ExecucaoServico>.Filter.ElemMatch(item => item.RespostasCancelamentoPago, item => item.PublicadaEm == null);
        return _execucoes.Find(filter).SortBy(item => item.CriadaEm).Limit(limite).ToList()
            .SelectMany(item => item.RespostasEntrada.Where(resposta => resposta.PublicadaEm == null)
                .Select(RespostaEntradaMapper.Mapear).Concat(item.EventosDiagnostico
                    .Where(evento => evento.PublicadoEm == null).Select(EventoDiagnosticoMapper.Mapear))
                .Concat(item.RespostasRecusaOrcamento.Where(resposta => resposta.PublicadaEm == null)
                    .Select(RespostaRecusaOrcamentoMapper.Mapear))
                .Concat(item.RespostasLiberacao.Where(resposta => resposta.PublicadaEm == null).Select(RespostaLiberacaoExecucaoMapper.Mapear))
                .Concat(item.EventosReparo.Where(evento => evento.PublicadoEm == null).Select(EventoReparoMapper.Mapear))
                .Concat(item.EventosConclusaoReparo.Where(evento => evento.PublicadoEm == null).Select(EventoConclusaoReparoMapper.Mapear))
                .Concat(item.RespostasCancelamentoPago.Where(resposta => resposta.PublicadaEm == null).Select(RespostaCancelamentoPagoMapper.Mapear)))
            .OrderBy(item => item.OccurredAt).ThenBy(item => item.MessageId).Take(limite).ToList();
    }

    public void ConfirmarPublicacao(Guid messageId)
    {
        var cancelamento = Builders<ExecucaoServico>.Filter.ElemMatch(item => item.RespostasCancelamentoPago,
            item => item.MessageId == messageId && item.PublicadaEm == null);
        _execucoes.UpdateOne(cancelamento, Builders<ExecucaoServico>.Update.Set("RespostasCancelamentoPago.$.PublicadaEm", DateTime.UtcNow).Inc(item => item.Versao, 1));
        var conclusao = Builders<ExecucaoServico>.Filter.ElemMatch(item => item.EventosConclusaoReparo,
            item => item.MessageId == messageId && item.PublicadoEm == null);
        _execucoes.UpdateOne(conclusao, Builders<ExecucaoServico>.Update.Set("EventosConclusaoReparo.$.PublicadoEm", DateTime.UtcNow).Inc(item => item.Versao, 1));
        var reparo = Builders<ExecucaoServico>.Filter.ElemMatch(item => item.EventosReparo,
            item => item.MessageId == messageId && item.PublicadoEm == null);
        _execucoes.UpdateOne(reparo, Builders<ExecucaoServico>.Update.Set("EventosReparo.$.PublicadoEm", DateTime.UtcNow).Inc(item => item.Versao, 1));
        var liberacao = Builders<ExecucaoServico>.Filter.ElemMatch(item => item.RespostasLiberacao,
            item => item.MessageId == messageId && item.PublicadaEm == null);
        _execucoes.UpdateOne(liberacao, Builders<ExecucaoServico>.Update.Set("RespostasLiberacao.$.PublicadaEm", DateTime.UtcNow).Inc(item => item.Versao, 1));
        var filter = Builders<ExecucaoServico>.Filter.ElemMatch(item => item.RespostasEntrada,
            item => item.MessageId == messageId && item.PublicadaEm == null);
        var update = Builders<ExecucaoServico>.Update.Set("RespostasEntrada.$.PublicadaEm", DateTime.UtcNow)
            .Inc(item => item.Versao, 1);
        _execucoes.UpdateOne(filter, update);
        filter = Builders<ExecucaoServico>.Filter.ElemMatch(item => item.RespostasRecusaOrcamento,
            item => item.MessageId == messageId && item.PublicadaEm == null);
        update = Builders<ExecucaoServico>.Update.Set("RespostasRecusaOrcamento.$.PublicadaEm", DateTime.UtcNow)
            .Inc(item => item.Versao, 1);
        _execucoes.UpdateOne(filter, update);
        filter = Builders<ExecucaoServico>.Filter.ElemMatch(item => item.EventosDiagnostico,
            item => item.MessageId == messageId && item.PublicadoEm == null);
        update = Builders<ExecucaoServico>.Update.Set("EventosDiagnostico.$.PublicadoEm", DateTime.UtcNow)
            .Inc(item => item.Versao, 1);
        _execucoes.UpdateOne(filter, update);
    }
}
