using OficinaMecanica.Execucao.Domain.Entities;

namespace OficinaMecanica.Execucao.InterfaceAdapters.DataSources;

public interface IPecaInsumoDataSource
{
    List<PecaInsumo> ObterTodos();
    PecaInsumo? ObterPorId(Guid id);
    bool ExistePorNome(string nome);
    bool ExistePorNomeExcetoId(string nome, Guid id);
    PecaInsumo Adicionar(PecaInsumo pecaInsumo);
    PecaInsumo? Atualizar(PecaInsumo pecaInsumo);
    bool Remover(Guid id);
    PecaInsumo? MovimentarEstoque(Guid id, int quantidade);
}
