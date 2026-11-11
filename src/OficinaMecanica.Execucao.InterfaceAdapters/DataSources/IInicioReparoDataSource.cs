using OficinaMecanica.Execucao.Domain.Entities;

namespace OficinaMecanica.Execucao.InterfaceAdapters.DataSources;

public interface IInicioReparoDataSource
{
    bool ReservarEIniciar(ExecucaoServico execucao, long versaoEsperada);
}
