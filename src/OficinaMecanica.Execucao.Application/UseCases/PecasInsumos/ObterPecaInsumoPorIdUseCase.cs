using OficinaMecanica.Execucao.Application.Inputs.PecasInsumos;
using OficinaMecanica.Execucao.Application.Outputs.PecasInsumos;
using OficinaMecanica.Execucao.Application.Mappers.PecasInsumos;
using OficinaMecanica.Execucao.Application.Gateways;

namespace OficinaMecanica.Execucao.Application.UseCases.PecasInsumos;

public class ObterPecaInsumoPorIdUseCase
{
    private readonly IPecaInsumoGateway _pecaInsumoGateway;

    public ObterPecaInsumoPorIdUseCase(IPecaInsumoGateway pecaInsumoGateway)
    {
        _pecaInsumoGateway = pecaInsumoGateway;
    }

    public PecaInsumoOutput? Executar(Guid id)
    {
        var pecaInsumo = _pecaInsumoGateway.ObterPorId(id);

        return pecaInsumo is null ? null : PecaInsumoOutputMapper.Mapear(pecaInsumo);
    }
}
