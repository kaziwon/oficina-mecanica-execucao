namespace OficinaMecanica.Execucao.Application.Outputs.Execucoes;

public class ItemServicoDiagnosticoOutput
{
    public Guid ServicoId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public decimal Preco { get; set; }
}
