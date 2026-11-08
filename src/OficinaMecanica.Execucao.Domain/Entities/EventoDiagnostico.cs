namespace OficinaMecanica.Execucao.Domain.Entities;

public class EventoDiagnostico
{
    public Guid MessageId { get; set; } = Guid.NewGuid();
    public string Tipo { get; set; } = string.Empty;
    public Guid SagaId { get; set; }
    public Guid CausationId { get; set; }
    public Guid OrdemServicoId { get; set; }
    public Guid ExecucaoId { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public DateTime CriadoEm { get; set; }
    public DateTime IniciadoEm { get; set; }
    public DateTime? ConcluidoEm { get; set; }
    public string? Diagnostico { get; set; }
    public List<ItemServicoDiagnostico> Servicos { get; set; } = [];
    public List<ItemPecaDiagnostico> PecasInsumos { get; set; } = [];
    public DateTime? PublicadoEm { get; set; }
}
