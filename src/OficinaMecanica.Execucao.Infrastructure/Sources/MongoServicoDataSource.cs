using MongoDB.Driver;
using OficinaMecanica.Execucao.Domain.Entities;
using OficinaMecanica.Execucao.Infrastructure.Persistence;
using OficinaMecanica.Execucao.InterfaceAdapters.DataSources;

namespace OficinaMecanica.Execucao.Infrastructure.Sources;

public class MongoServicoDataSource : IServicoDataSource
{
    private readonly IMongoCollection<Servico> _servicos;
    public MongoServicoDataSource(MongoDbContext context) { _servicos = context.Servicos; }
    public List<Servico> ObterTodos() => _servicos.Find(item => item.Ativo).SortBy(item => item.Nome).ToList();
    public Servico? ObterPorId(Guid id) => _servicos.Find(item => item.Id == id && item.Ativo).FirstOrDefault();
    public bool ExistePorNome(string nome) => _servicos.Find(item => item.Nome == nome.Trim(),
        new FindOptions { Collation = MongoDbContext.NameCollation }).Any();
    public bool ExistePorNomeExcetoId(string nome, Guid id) => _servicos.Find(item => item.Nome == nome.Trim() && item.Id != id,
        new FindOptions { Collation = MongoDbContext.NameCollation }).Any();
    public Servico Adicionar(Servico servico) { _servicos.InsertOne(servico); return servico; }
    public Servico? Atualizar(Servico servico) => _servicos.FindOneAndUpdate(
        item => item.Id == servico.Id && item.Ativo,
        Builders<Servico>.Update.Set(item => item.Nome, servico.Nome)
            .Set(item => item.Descricao, servico.Descricao).Set(item => item.Preco, servico.Preco),
        new FindOneAndUpdateOptions<Servico> { ReturnDocument = ReturnDocument.After });
    public bool Remover(Guid id) => _servicos.UpdateOne(item => item.Id == id && item.Ativo,
        Builders<Servico>.Update.Set(item => item.Ativo, false)).ModifiedCount == 1;
}
