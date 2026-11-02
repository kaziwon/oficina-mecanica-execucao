namespace OficinaMecanica.Execucao.InterfaceAdapters.DTOs;

public class ItemServicoDiagnosticoResponseDto
{
    public Guid ServicoId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public decimal Preco { get; set; }
}
