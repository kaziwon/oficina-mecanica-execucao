using OficinaMecanica.Execucao.Application.Inputs.PecasInsumos;
using OficinaMecanica.Execucao.Application.Mappers.PecasInsumos;
namespace OficinaMecanica.Execucao.Application.Outputs.PecasInsumos;

public class PecaInsumoOutput
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public decimal PrecoUnitario { get; set; }
    public int QuantidadeEstoque { get; set; }
    public int QuantidadeReservada { get; set; }
    public int QuantidadeDisponivel { get; set; }
}
