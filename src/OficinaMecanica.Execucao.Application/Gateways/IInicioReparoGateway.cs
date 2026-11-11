using OficinaMecanica.Execucao.Domain.Entities;

namespace OficinaMecanica.Execucao.Application.Gateways;

public interface IInicioReparoGateway
{
    bool ReservarEIniciar(ExecucaoServico execucao, long versaoEsperada);
}
