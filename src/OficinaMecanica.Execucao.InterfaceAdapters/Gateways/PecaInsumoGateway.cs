using OficinaMecanica.Execucao.Application.Gateways;
using OficinaMecanica.Execucao.Domain.Entities;
using OficinaMecanica.Execucao.InterfaceAdapters.DataSources;

namespace OficinaMecanica.Execucao.InterfaceAdapters.Gateways;

public class PecaInsumoGateway : IPecaInsumoGateway
{
    public PecaInsumo? MovimentarEstoque(Guid id, int quantidade)
    {
        return _pecaInsumoDataSource.MovimentarEstoque(id, quantidade);
    }

    private readonly IPecaInsumoDataSource _pecaInsumoDataSource;

    public PecaInsumoGateway(IPecaInsumoDataSource pecaInsumoDataSource)
    {
        _pecaInsumoDataSource = pecaInsumoDataSource;
    }

    public List<PecaInsumo> ObterTodos()
    {
        return _pecaInsumoDataSource.ObterTodos();
    }

    public PecaInsumo? ObterPorId(Guid id)
    {
        return _pecaInsumoDataSource.ObterPorId(id);
    }

    public bool ExistePorNome(string nome)
    {
        return _pecaInsumoDataSource.ExistePorNome(nome);
    }

    public bool ExistePorNomeExcetoId(string nome, Guid id)
    {
        return _pecaInsumoDataSource.ExistePorNomeExcetoId(nome, id);
    }

    public PecaInsumo Adicionar(PecaInsumo pecaInsumo)
    {
        return _pecaInsumoDataSource.Adicionar(pecaInsumo);
    }

    public PecaInsumo? Atualizar(PecaInsumo pecaInsumo)
    {
        return _pecaInsumoDataSource.Atualizar(pecaInsumo);
    }

    public bool Remover(Guid id)
    {
        return _pecaInsumoDataSource.Remover(id);
    }
}
