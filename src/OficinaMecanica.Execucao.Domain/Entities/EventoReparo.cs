namespace OficinaMecanica.Execucao.Domain.Entities;

public class EventoReparo
{
    public Guid MessageId { get; set; } = Guid.NewGuid();
    public Guid SagaId { get; set; }
    public Guid CausationId { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public Guid OrdemServicoId { get; set; }
    public Guid ExecucaoId { get; set; }
    public Guid OrcamentoId { get; set; }
    public Guid PagamentoId { get; set; }
    public string ProvedorId { get; set; } = string.Empty;
    public decimal ValorPago { get; set; }
    public DateTime IniciadoEm { get; set; }
    public DateTime? PublicadoEm { get; set; }
}
