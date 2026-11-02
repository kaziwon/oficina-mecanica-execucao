namespace OficinaMecanica.Execucao.Domain.Entities;

public class ItemPecaDiagnostico
{
    public Guid PecaInsumoId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public int Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }
}
