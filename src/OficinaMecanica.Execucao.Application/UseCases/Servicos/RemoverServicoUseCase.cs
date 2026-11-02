using OficinaMecanica.Execucao.Application.Inputs.Servicos;
using OficinaMecanica.Execucao.Application.Outputs.Servicos;
using OficinaMecanica.Execucao.Application.Mappers.Servicos;
using OficinaMecanica.Execucao.Application.Gateways;

namespace OficinaMecanica.Execucao.Application.UseCases.Servicos;

public class RemoverServicoUseCase
{
    private readonly IServicoGateway _servicoGateway;

    public RemoverServicoUseCase(IServicoGateway servicoGateway)
    {
        _servicoGateway = servicoGateway;
    }

    public bool Executar(Guid id)
    {
        return _servicoGateway.Remover(id);
    }
}
