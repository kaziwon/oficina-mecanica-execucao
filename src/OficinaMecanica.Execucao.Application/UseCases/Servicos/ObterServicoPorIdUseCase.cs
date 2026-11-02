using OficinaMecanica.Execucao.Application.Inputs.Servicos;
using OficinaMecanica.Execucao.Application.Outputs.Servicos;
using OficinaMecanica.Execucao.Application.Mappers.Servicos;
using OficinaMecanica.Execucao.Application.Gateways;

namespace OficinaMecanica.Execucao.Application.UseCases.Servicos;

public class ObterServicoPorIdUseCase
{
    private readonly IServicoGateway _servicoGateway;

    public ObterServicoPorIdUseCase(IServicoGateway servicoGateway)
    {
        _servicoGateway = servicoGateway;
    }

    public ServicoOutput? Executar(Guid id)
    {
        var servico = _servicoGateway.ObterPorId(id);

        return servico is null ? null : ServicoOutputMapper.Mapear(servico);
    }
}
