namespace OficinaMecanica.Execucao.Domain.Entities;

public class AutorizacaoExecucao
{
    public Guid OrcamentoId { get; set; }
    public Guid PagamentoId { get; set; }
    public string ProvedorId { get; set; } = string.Empty;
    public decimal ValorPago { get; set; }
    public Guid MensagemLiberacaoId { get; set; }
    public Guid ConfirmacaoPagamentoId { get; set; }
    public DateTime LiberadaEm { get; set; }
}
