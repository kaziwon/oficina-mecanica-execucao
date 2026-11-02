using OficinaMecanica.Execucao.Application.Gateways;
using OficinaMecanica.Execucao.Domain.Entities;
using OficinaMecanica.Execucao.InterfaceAdapters.DataSources;

namespace OficinaMecanica.Execucao.InterfaceAdapters.Gateways;

public class ServicoGateway : IServicoGateway
{
    private readonly IServicoDataSource _servicoDataSource;

    public ServicoGateway(IServicoDataSource servicoDataSource)
    {
        _servicoDataSource = servicoDataSource;
    }

    public List<Servico> ObterTodos()
    {
        return _servicoDataSource.ObterTodos();
    }

    public Servico? ObterPorId(Guid id)
    {
        return _servicoDataSource.ObterPorId(id);
    }

    public bool ExistePorNome(string nome)
    {
        return _servicoDataSource.ExistePorNome(nome);
    }

    public bool ExistePorNomeExcetoId(string nome, Guid id)
    {
        return _servicoDataSource.ExistePorNomeExcetoId(nome, id);
    }

    public Servico Adicionar(Servico servico)
    {
        return _servicoDataSource.Adicionar(servico);
    }

    public Servico? Atualizar(Servico servico)
    {
        return _servicoDataSource.Atualizar(servico);
    }

    public bool Remover(Guid id)
    {
        return _servicoDataSource.Remover(id);
    }
}
