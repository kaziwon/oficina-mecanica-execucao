using OficinaMecanica.Execucao.Domain.Entities;

namespace OficinaMecanica.Execucao.InterfaceAdapters.DTOs;

public class ExecucaoResponseDto
{
    public Guid Id { get; set; }
    public Guid OrdemServicoId { get; set; }
    public string DescricaoSolicitacao { get; set; } = string.Empty;
    public string Diagnostico { get; set; } = string.Empty;
    public StatusExecucao Status { get; set; }
    public DateTime CriadaEm { get; set; }
    public DateTime? DiagnosticoIniciadoEm { get; set; }
    public DateTime? DiagnosticoConcluidoEm { get; set; }
    public long Versao { get; set; }
    public List<ItemServicoDiagnosticoResponseDto> Servicos { get; set; } = [];
    public List<ItemPecaDiagnosticoResponseDto> PecasInsumos { get; set; } = [];
    public List<HistoricoExecucaoResponseDto> Historico { get; set; } = [];
}
