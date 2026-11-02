using OficinaMecanica.Execucao.Application.Inputs.PecasInsumos;
using OficinaMecanica.Execucao.Application.Outputs.PecasInsumos;
using OficinaMecanica.Execucao.Domain.Entities;

namespace OficinaMecanica.Execucao.Application.Mappers.PecasInsumos;

internal static class PecaInsumoOutputMapper
{
    public static PecaInsumoOutput Mapear(PecaInsumo pecaInsumo)
    {
        return new PecaInsumoOutput
        {
            Id = pecaInsumo.Id,
            Nome = pecaInsumo.Nome,
            Descricao = pecaInsumo.Descricao,
            PrecoUnitario = pecaInsumo.PrecoUnitario,
            QuantidadeEstoque = pecaInsumo.QuantidadeEstoque
        };
    }
}
