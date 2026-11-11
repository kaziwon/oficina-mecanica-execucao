using OficinaMecanica.Execucao.Application.Outputs.PecasInsumos;
using OficinaMecanica.Execucao.InterfaceAdapters.DTOs;

namespace OficinaMecanica.Execucao.InterfaceAdapters.Presenters;

public static class PecaInsumoPresenter
{
    public static PecaInsumoResponseDto Apresentar(PecaInsumoOutput output) => new()
    {
        Id = output.Id,
        Nome = output.Nome,
        Descricao = output.Descricao,
        PrecoUnitario = output.PrecoUnitario,
        QuantidadeEstoque = output.QuantidadeEstoque,
        QuantidadeReservada = output.QuantidadeReservada,
        QuantidadeDisponivel = output.QuantidadeDisponivel
    };
}
