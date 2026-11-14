using OficinaMecanica.Execucao.Domain.Entities;
namespace OficinaMecanica.Execucao.Application.Gateways;

public interface ICancelamentoPagoGateway
{
    bool LiberarECancelar(ExecucaoServico execucao, long versaoEsperada);
}
