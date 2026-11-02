namespace OficinaMecanica.Execucao.Application.Outputs.Execucoes;

public class ItemPecaDiagnosticoOutput
{
    public Guid PecaInsumoId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public int Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }
}
