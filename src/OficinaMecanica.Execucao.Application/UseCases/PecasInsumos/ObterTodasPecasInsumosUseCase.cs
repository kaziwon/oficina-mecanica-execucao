using OficinaMecanica.Execucao.Application.Inputs.PecasInsumos;
using OficinaMecanica.Execucao.Application.Outputs.PecasInsumos;
using OficinaMecanica.Execucao.Application.Mappers.PecasInsumos;
using OficinaMecanica.Execucao.Application.Gateways;

namespace OficinaMecanica.Execucao.Application.UseCases.PecasInsumos;

public class ObterTodasPecasInsumosUseCase
{
    private readonly IPecaInsumoGateway _pecaInsumoGateway;

    public ObterTodasPecasInsumosUseCase(IPecaInsumoGateway pecaInsumoGateway)
    {
        _pecaInsumoGateway = pecaInsumoGateway;
    }

    public List<PecaInsumoOutput> Executar()
    {
        return _pecaInsumoGateway.ObterTodos().Select(PecaInsumoOutputMapper.Mapear).ToList();
    }
}
