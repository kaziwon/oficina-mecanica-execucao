namespace OficinaMecanica.Execucao.InterfaceAdapters.DTOs;

public class ServicoResponseDto
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public decimal Preco { get; set; }
}
