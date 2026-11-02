using OficinaMecanica.Execucao.Domain.Entities;

namespace OficinaMecanica.Execucao.Application.Outputs.Execucoes;

public class HistoricoExecucaoOutput
{
    public StatusExecucao Status { get; set; }
    public DateTime RegistradoEm { get; set; }
}
