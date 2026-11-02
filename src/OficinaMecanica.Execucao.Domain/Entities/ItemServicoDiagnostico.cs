namespace OficinaMecanica.Execucao.Domain.Entities;

public class ItemServicoDiagnostico
{
    public Guid ServicoId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public decimal Preco { get; set; }
}
