using OficinaMecanica.Execucao.Application.Outputs.Servicos;
using OficinaMecanica.Execucao.InterfaceAdapters.DTOs;

namespace OficinaMecanica.Execucao.InterfaceAdapters.Presenters;

public static class ServicoPresenter
{
    public static ServicoResponseDto Apresentar(ServicoOutput output) => new()
    {
        Id = output.Id,
        Nome = output.Nome,
        Descricao = output.Descricao,
        Preco = output.Preco
    };
}
