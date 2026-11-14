using OficinaMecanica.Execucao.Domain.Entities;
namespace OficinaMecanica.Execucao.InterfaceAdapters.DataSources;

public interface ICancelamentoPagoDataSource
{
    bool LiberarECancelar(ExecucaoServico execucao, long versaoEsperada);
}
