using OficinaMecanica.Execucao.Domain.Entities;

namespace OficinaMecanica.Execucao.Application.Outputs.Execucoes;

public class ExecucaoOutput
{
    public Guid Id { get; set; }
    public Guid OrdemServicoId { get; set; }
    public string DescricaoSolicitacao { get; set; } = string.Empty;
    public string Diagnostico { get; set; } = string.Empty;
    public StatusExecucao Status { get; set; }
    public DateTime CriadaEm { get; set; }
    public DateTime? DiagnosticoIniciadoEm { get; set; }
    public DateTime? DiagnosticoConcluidoEm { get; set; }
    public Guid? OrcamentoId { get; set; }
    public Guid? PagamentoId { get; set; }
    public DateTime? LiberadaEm { get; set; }
    public DateTime? ReparoIniciadoEm { get; set; }
    public DateTime? ReparoConcluidoEm { get; set; }
    public DateTime? ReparoCanceladoEm { get; set; }
    public List<ReservaPecaOutput> ReservasPecas { get; set; } = [];
    public long Versao { get; set; }
    public List<ItemServicoDiagnosticoOutput> Servicos { get; set; } = [];
    public List<ItemPecaDiagnosticoOutput> PecasInsumos { get; set; } = [];
    public List<HistoricoExecucaoOutput> Historico { get; set; } = [];
}
