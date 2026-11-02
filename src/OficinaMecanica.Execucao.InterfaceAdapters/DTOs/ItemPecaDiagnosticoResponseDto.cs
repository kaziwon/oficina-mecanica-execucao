namespace OficinaMecanica.Execucao.InterfaceAdapters.DTOs;

public class ItemPecaDiagnosticoResponseDto
{
    public Guid PecaInsumoId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public int Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }
}
