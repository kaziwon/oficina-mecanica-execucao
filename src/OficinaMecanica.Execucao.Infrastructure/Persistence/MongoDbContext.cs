using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using OficinaMecanica.Execucao.Domain.Entities;
using OficinaMecanica.Execucao.Infrastructure.Settings;

namespace OficinaMecanica.Execucao.Infrastructure.Persistence;

public class MongoDbContext
{
    public static Collation NameCollation { get; } = new("pt", strength: CollationStrength.Secondary);
    private readonly IMongoDatabase _database;
    public IMongoCollection<Servico> Servicos { get; }
    public IMongoCollection<PecaInsumo> Pecas { get; }
    public IMongoCollection<ExecucaoServico> Execucoes { get; }

    public MongoDbContext(IMongoClient client, IOptions<MongoSettings> settings)
    {
        MongoSerialization.Configure();
        _database = client.GetDatabase(settings.Value.DatabaseName);
        Servicos = _database.GetCollection<Servico>("servicos");
        Pecas = _database.GetCollection<PecaInsumo>("pecas_insumos");
        Execucoes = _database.GetCollection<ExecucaoServico>("execucoes");
    }

    public async Task CriarIndices(CancellationToken cancellationToken = default)
    {
        await Servicos.Indexes.CreateOneAsync(new CreateIndexModel<Servico>(
            Builders<Servico>.IndexKeys.Ascending(item => item.Nome),
            new CreateIndexOptions { Unique = true, Collation = NameCollation }), cancellationToken: cancellationToken);
        await Pecas.Indexes.CreateOneAsync(new CreateIndexModel<PecaInsumo>(
            Builders<PecaInsumo>.IndexKeys.Ascending(item => item.Nome),
            new CreateIndexOptions { Unique = true, Collation = NameCollation }), cancellationToken: cancellationToken);
        await Execucoes.Indexes.CreateManyAsync([
            new CreateIndexModel<ExecucaoServico>(Builders<ExecucaoServico>.IndexKeys.Ascending(item => item.OrdemServicoId),
                new CreateIndexOptions { Unique = true }),
            new CreateIndexModel<ExecucaoServico>(Builders<ExecucaoServico>.IndexKeys
                .Ascending(item => item.Status).Ascending(item => item.CriadaEm)),
            new CreateIndexModel<ExecucaoServico>(Builders<ExecucaoServico>.IndexKeys.Ascending(item => item.CriadaEm))
        ], cancellationToken);
    }

    public async Task<bool> EstaDisponivel(CancellationToken cancellationToken)
    {
        try
        {
            await _database.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1), cancellationToken: cancellationToken);
            return true;
        }
        catch (MongoException) { return false; }
        catch (TimeoutException) { return false; }
    }
}
