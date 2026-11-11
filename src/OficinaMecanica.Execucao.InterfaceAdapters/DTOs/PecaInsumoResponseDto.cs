namespace OficinaMecanica.Execucao.InterfaceAdapters.DTOs;

public class PecaInsumoResponseDto
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public decimal PrecoUnitario { get; set; }
    public int QuantidadeEstoque { get; set; }
    public int QuantidadeReservada { get; set; }
    public int QuantidadeDisponivel { get; set; }
}
