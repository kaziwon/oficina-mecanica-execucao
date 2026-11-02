using OficinaMecanica.Execucao.Domain.Entities;

namespace OficinaMecanica.Execucao.Application.Gateways;

public interface IServicoGateway
{
    List<Servico> ObterTodos();
    Servico? ObterPorId(Guid id);
    bool ExistePorNome(string nome);
    bool ExistePorNomeExcetoId(string nome, Guid id);
    Servico Adicionar(Servico servico);
    Servico? Atualizar(Servico servico);
    bool Remover(Guid id);
}
