namespace OficinaMecanica.Execucao.Domain.Entities;

public class PecaInsumo
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public decimal PrecoUnitario { get; set; }
    public int QuantidadeEstoque { get; set; }
    public bool Ativo { get; set; } = true;
}
