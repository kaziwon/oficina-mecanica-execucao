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
        var filter = Builders<ExecucaoServico>.Filter.ElemMatch(item => item.RespostasEntrada, item => item.PublicadaEm == null);
        return _execucoes.Find(filter).SortBy(item => item.CriadaEm).Limit(limite).ToList()
            .SelectMany(item => item.RespostasEntrada).Where(item => item.PublicadaEm == null)
            .OrderBy(item => item.CriadaEm).Take(limite).Select(RespostaEntradaMapper.Mapear).ToList();
    }

    public void ConfirmarPublicacao(Guid messageId)
    {
        var filter = Builders<ExecucaoServico>.Filter.ElemMatch(item => item.RespostasEntrada,
            item => item.MessageId == messageId && item.PublicadaEm == null);
        var update = Builders<ExecucaoServico>.Update.Set("RespostasEntrada.$.PublicadaEm", DateTime.UtcNow)
            .Inc(item => item.Versao, 1);
        _execucoes.UpdateOne(filter, update);
    }
}
