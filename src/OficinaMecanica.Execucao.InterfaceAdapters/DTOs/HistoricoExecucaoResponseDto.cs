using OficinaMecanica.Execucao.Domain.Entities;

namespace OficinaMecanica.Execucao.InterfaceAdapters.DTOs;

public class HistoricoExecucaoResponseDto
{
    public StatusExecucao Status { get; set; }
    public DateTime RegistradoEm { get; set; }
}
