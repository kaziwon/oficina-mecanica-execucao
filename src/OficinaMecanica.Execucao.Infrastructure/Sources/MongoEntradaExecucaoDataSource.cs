using MongoDB.Driver;
using OficinaMecanica.Execucao.Domain.Entities;
using OficinaMecanica.Execucao.Infrastructure.Persistence;
using OficinaMecanica.Execucao.InterfaceAdapters.DataSources;

namespace OficinaMecanica.Execucao.Infrastructure.Sources;

public class MongoEntradaExecucaoDataSource : IEntradaExecucaoDataSource
{
    private readonly IMongoCollection<ExecucaoServico> _execucoes;
    public MongoEntradaExecucaoDataSource(MongoDbContext database) { _execucoes = database.Execucoes; }
    public ExecucaoServico? ObterPorOrdemServicoId(Guid id) => _execucoes.Find(item => item.OrdemServicoId == id).FirstOrDefault();
    public bool TentarInserir(ExecucaoServico execucao)
    {
        try { _execucoes.InsertOne(execucao); return true; }
        catch (MongoWriteException exception) when (exception.WriteError.Category == ServerErrorCategory.DuplicateKey) { return false; }
    }
    public bool TentarAtualizar(ExecucaoServico execucao, long versaoEsperada)
        => _execucoes.ReplaceOne(item => item.Id == execucao.Id && item.Versao == versaoEsperada, execucao).MatchedCount == 1;
}
