using OficinaMecanica.Execucao.Domain.Entities;

namespace OficinaMecanica.Execucao.Application.Gateways;

public interface IConclusaoReparoGateway
{
    bool ConsumirEConcluir(ExecucaoServico execucao, long versaoEsperada);
}
