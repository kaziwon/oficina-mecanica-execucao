using OficinaMecanica.Execucao.Domain.Entities;

namespace OficinaMecanica.Execucao.InterfaceAdapters.DataSources;

public interface IConclusaoReparoDataSource
{
    bool ConsumirEConcluir(ExecucaoServico execucao, long versaoEsperada);
}
