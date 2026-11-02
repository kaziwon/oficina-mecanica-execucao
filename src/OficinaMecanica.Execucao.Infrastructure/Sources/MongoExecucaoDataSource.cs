using MongoDB.Driver;
using OficinaMecanica.Execucao.Domain.Entities;
using OficinaMecanica.Execucao.Infrastructure.Persistence;
using OficinaMecanica.Execucao.InterfaceAdapters.DataSources;

namespace OficinaMecanica.Execucao.Infrastructure.Sources;

public class MongoExecucaoDataSource : IExecucaoDataSource
{
    private readonly IMongoCollection<ExecucaoServico> _execucoes;
    public MongoExecucaoDataSource(MongoDbContext context) { _execucoes = context.Execucoes; }
    public ExecucaoServico Adicionar(ExecucaoServico execucao) { _execucoes.InsertOne(execucao); return execucao; }
    public ExecucaoServico? ObterPorId(Guid id) => _execucoes.Find(item => item.Id == id).FirstOrDefault();
    public ExecucaoServico? ObterPorOrdemServicoId(Guid id) => _execucoes.Find(item => item.OrdemServicoId == id).FirstOrDefault();
    public List<ExecucaoServico> ObterFila(StatusExecucao? status)
    {
        var filter = Builders<ExecucaoServico>.Filter.Ne(item => item.Status, StatusExecucao.Finalizada)
            & Builders<ExecucaoServico>.Filter.Ne(item => item.Status, StatusExecucao.Cancelada);
        if (status.HasValue) filter &= Builders<ExecucaoServico>.Filter.Eq(item => item.Status, status.Value);
        return _execucoes.Find(filter).SortBy(item => item.CriadaEm).ThenBy(item => item.Id).ToList();
    }
    public bool Atualizar(ExecucaoServico execucao, long versaoEsperada)
    {
        return _execucoes.ReplaceOne(item => item.Id == execucao.Id && item.Versao == versaoEsperada,
            execucao).MatchedCount == 1;
    }
}
