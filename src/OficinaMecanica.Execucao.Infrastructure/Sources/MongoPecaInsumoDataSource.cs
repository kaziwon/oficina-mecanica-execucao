using MongoDB.Bson;
using MongoDB.Driver;
using OficinaMecanica.Execucao.Domain.Exceptions;
using OficinaMecanica.Execucao.Domain.Entities;
using OficinaMecanica.Execucao.Infrastructure.Persistence;
using OficinaMecanica.Execucao.InterfaceAdapters.DataSources;

namespace OficinaMecanica.Execucao.Infrastructure.Sources;

public class MongoPecaInsumoDataSource : IPecaInsumoDataSource
{
    private readonly IMongoCollection<PecaInsumo> _pecas;
    public MongoPecaInsumoDataSource(MongoDbContext context) { _pecas = context.Pecas; }
    public List<PecaInsumo> ObterTodos() => _pecas.Find(item => item.Ativo).SortBy(item => item.Nome).ToList();
    public PecaInsumo? ObterPorId(Guid id) => _pecas.Find(item => item.Id == id && item.Ativo).FirstOrDefault();
    public bool ExistePorNome(string nome) => _pecas.Find(item => item.Nome == nome.Trim(),
        new FindOptions { Collation = MongoDbContext.NameCollation }).Any();
    public bool ExistePorNomeExcetoId(string nome, Guid id) => _pecas.Find(item => item.Nome == nome.Trim() && item.Id != id,
        new FindOptions { Collation = MongoDbContext.NameCollation }).Any();
    public PecaInsumo Adicionar(PecaInsumo peca) { _pecas.InsertOne(peca); return peca; }
    public PecaInsumo? Atualizar(PecaInsumo peca) => _pecas.FindOneAndUpdate(item => item.Id == peca.Id && item.Ativo,
        Builders<PecaInsumo>.Update.Set(item => item.Nome, peca.Nome)
            .Set(item => item.Descricao, peca.Descricao).Set(item => item.PrecoUnitario, peca.PrecoUnitario),
        new FindOneAndUpdateOptions<PecaInsumo> { ReturnDocument = ReturnDocument.After });
    public bool Remover(Guid id)
    {
        var semReserva = Builders<PecaInsumo>.Filter.Eq(item => item.QuantidadeReservada, 0)
            | Builders<PecaInsumo>.Filter.Exists(item => item.QuantidadeReservada, false);
        var removida = _pecas.UpdateOne(Builders<PecaInsumo>.Filter.Where(item => item.Id == id && item.Ativo) & semReserva,
            Builders<PecaInsumo>.Update.Set(item => item.Ativo, false)).ModifiedCount == 1;
        if (!removida && _pecas.Find(item => item.Id == id && item.Ativo).Any())
            throw new RegraNegocioException("Pecas com reservas ativas nao podem ser desativadas.");
        return removida;
    }

    public PecaInsumo? MovimentarEstoque(Guid id, int quantidade)
    {
        if (quantidade == 0 || quantidade == int.MinValue) return null;
        var filter = Builders<PecaInsumo>.Filter;
        var saldoValido = quantidade < 0 ? new BsonDocumentFilterDefinition<PecaInsumo>(new BsonDocument("$expr",
            new BsonDocument("$gte", new BsonArray {
                new BsonDocument("$subtract", new BsonArray { "$QuantidadeEstoque",
                    new BsonDocument("$ifNull", new BsonArray { "$QuantidadeReservada", 0 }) }), -quantidade })))
            : filter.Lte(item => item.QuantidadeEstoque, int.MaxValue - quantidade);
        return _pecas.FindOneAndUpdate(filter.Eq(item => item.Id, id) & filter.Eq(item => item.Ativo, true) & saldoValido,
            Builders<PecaInsumo>.Update.Inc(item => item.QuantidadeEstoque, quantidade),
            new FindOneAndUpdateOptions<PecaInsumo> { ReturnDocument = ReturnDocument.After });
    }
}
