using OficinaMecanica.Execucao.Application.Inputs.PecasInsumos;
using OficinaMecanica.Execucao.Application.Outputs.PecasInsumos;
using OficinaMecanica.Execucao.Application.Mappers.PecasInsumos;
using OficinaMecanica.Execucao.Application.Gateways;

namespace OficinaMecanica.Execucao.Application.UseCases.PecasInsumos;

public class RemoverPecaInsumoUseCase
{
    private readonly IPecaInsumoGateway _pecaInsumoGateway;

    public RemoverPecaInsumoUseCase(IPecaInsumoGateway pecaInsumoGateway)
    {
        _pecaInsumoGateway = pecaInsumoGateway;
    }

    public bool Executar(Guid id)
    {
        return _pecaInsumoGateway.Remover(id);
    }
}
