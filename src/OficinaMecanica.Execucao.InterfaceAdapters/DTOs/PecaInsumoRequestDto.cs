namespace OficinaMecanica.Execucao.InterfaceAdapters.DTOs;

public class PecaInsumoRequestDto
{
    public string Nome { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public decimal PrecoUnitario { get; set; }
    public int QuantidadeEstoque { get; set; }
}
