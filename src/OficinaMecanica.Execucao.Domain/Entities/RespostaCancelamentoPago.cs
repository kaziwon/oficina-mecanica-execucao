namespace OficinaMecanica.Execucao.Domain.Entities;

public class RespostaCancelamentoPago
{
    public Guid MessageId { get; set; } = Guid.NewGuid();
    public Guid SagaId { get; set; }
    public Guid CausationId { get; set; }
    public Guid CausaComando { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public Guid OrdemServicoId { get; set; }
    public Guid ExecucaoId { get; set; }
    public Guid OrcamentoId { get; set; }
    public Guid PagamentoId { get; set; }
    public string ProvedorId { get; set; } = string.Empty;
    public decimal ValorPago { get; set; }
    public string MotivoComando { get; set; } = string.Empty;
    public bool Aceito { get; set; }
    public string? Motivo { get; set; }
    public DateTime CriadaEm { get; set; } = DateTime.UtcNow;
    public DateTime? PublicadaEm { get; set; }
}
