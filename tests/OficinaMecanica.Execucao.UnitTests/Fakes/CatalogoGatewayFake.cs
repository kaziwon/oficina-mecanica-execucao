using System.Text.Json;
using OficinaMecanica.Execucao.Application.Gateways;
using OficinaMecanica.Execucao.Domain.Entities;

namespace OficinaMecanica.Execucao.UnitTests.Fakes;

public class CatalogoGatewayFake : IServicoGateway, IPecaInsumoGateway
{
    public List<Servico> Servicos { get; } = [];
    public List<PecaInsumo> Pecas { get; } = [];
    private static T? Copiar<T>(T? item) where T : class => item is null ? null : JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(item));
    List<Servico> IServicoGateway.ObterTodos() => Servicos.Where(item => item.Ativo).Select(item => Copiar(item)!).ToList();
    List<PecaInsumo> IPecaInsumoGateway.ObterTodos() => Pecas.Where(item => item.Ativo).Select(item => Copiar(item)!).ToList();
    Servico? IServicoGateway.ObterPorId(Guid id) => Copiar(Servicos.FirstOrDefault(item => item.Id == id && item.Ativo));
    PecaInsumo? IPecaInsumoGateway.ObterPorId(Guid id) => Copiar(Pecas.FirstOrDefault(item => item.Id == id && item.Ativo));
    bool IServicoGateway.ExistePorNome(string nome) => Servicos.Any(item => item.Nome.Equals(nome.Trim(), StringComparison.OrdinalIgnoreCase));
    bool IPecaInsumoGateway.ExistePorNome(string nome) => Pecas.Any(item => item.Nome.Equals(nome.Trim(), StringComparison.OrdinalIgnoreCase));
    bool IServicoGateway.ExistePorNomeExcetoId(string nome, Guid id) => Servicos.Any(item => item.Id != id && item.Nome.Equals(nome.Trim(), StringComparison.OrdinalIgnoreCase));
    bool IPecaInsumoGateway.ExistePorNomeExcetoId(string nome, Guid id) => Pecas.Any(item => item.Id != id && item.Nome.Equals(nome.Trim(), StringComparison.OrdinalIgnoreCase));
    public Servico Adicionar(Servico item) { Servicos.Add(Copiar(item)!); return Copiar(item)!; }
    public PecaInsumo Adicionar(PecaInsumo item) { Pecas.Add(Copiar(item)!); return Copiar(item)!; }
    public Servico? Atualizar(Servico item)
    {
        var index = Servicos.FindIndex(existing => existing.Id == item.Id && existing.Ativo);
        if (index < 0) return null;
        Servicos[index] = Copiar(item)!;
        return Copiar(item);
    }
    public PecaInsumo? Atualizar(PecaInsumo item)
    {
        var index = Pecas.FindIndex(existing => existing.Id == item.Id && existing.Ativo);
        if (index < 0) return null;
        item.QuantidadeEstoque = Pecas[index].QuantidadeEstoque;
        Pecas[index] = Copiar(item)!;
        return Copiar(item);
    }
    bool IServicoGateway.Remover(Guid id)
    {
        var item = Servicos.FirstOrDefault(existing => existing.Id == id && existing.Ativo);
        if (item is null) return false;
        item.Ativo = false;
        return true;
    }
    bool IPecaInsumoGateway.Remover(Guid id)
    {
        var item = Pecas.FirstOrDefault(existing => existing.Id == id && existing.Ativo);
        if (item is null) return false;
        item.Ativo = false;
        return true;
    }
    public PecaInsumo? MovimentarEstoque(Guid id, int quantidade)
    {
        var item = Pecas.FirstOrDefault(existing => existing.Id == id && existing.Ativo);
        if (item is null || (long)item.QuantidadeEstoque + quantidade is < 0 or > int.MaxValue) return null;
        item.QuantidadeEstoque += quantidade;
        return Copiar(item);
    }
}
