using OficinaMecanica.Execucao.Application.Gateways;
using OficinaMecanica.Execucao.Domain.Entities;
using OficinaMecanica.Execucao.InterfaceAdapters.DataSources;
namespace OficinaMecanica.Execucao.InterfaceAdapters.Gateways;

public class CancelamentoPagoGateway : ICancelamentoPagoGateway
{
    private readonly ICancelamentoPagoDataSource _source;
    public CancelamentoPagoGateway(ICancelamentoPagoDataSource source) { _source = source; }
    public bool LiberarECancelar(ExecucaoServico execucao, long versaoEsperada) => _source.LiberarECancelar(execucao, versaoEsperada);
}
