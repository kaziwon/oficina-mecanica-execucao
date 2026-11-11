namespace OficinaMecanica.Execucao.Domain.Entities;

public class RespostaRecusaOrcamento
{
    public Guid MessageId { get; set; } = Guid.NewGuid();
    public Guid SagaId { get; set; }
    public Guid CausationId { get; set; }
    public Guid CausaComando { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public Guid OrdemServicoId { get; set; }
    public Guid ExecucaoId { get; set; }
    public Guid OrcamentoId { get; set; }
    public string MotivoComando { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string? Codigo { get; set; }
    public string? Motivo { get; set; }
    public DateTime CriadaEm { get; set; } = DateTime.UtcNow;
    public DateTime? PublicadaEm { get; set; }
}
