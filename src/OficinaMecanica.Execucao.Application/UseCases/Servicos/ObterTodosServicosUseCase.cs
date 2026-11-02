using OficinaMecanica.Execucao.Application.Inputs.Servicos;
using OficinaMecanica.Execucao.Application.Outputs.Servicos;
using OficinaMecanica.Execucao.Application.Mappers.Servicos;
using OficinaMecanica.Execucao.Application.Gateways;

namespace OficinaMecanica.Execucao.Application.UseCases.Servicos;

public class ObterTodosServicosUseCase
{
    private readonly IServicoGateway _servicoGateway;

    public ObterTodosServicosUseCase(IServicoGateway servicoGateway)
    {
        _servicoGateway = servicoGateway;
    }

    public List<ServicoOutput> Executar()
    {
        return _servicoGateway.ObterTodos().Select(ServicoOutputMapper.Mapear).ToList();
    }
}
