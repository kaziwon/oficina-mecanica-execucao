namespace OficinaMecanica.Execucao.InterfaceAdapters.DTOs;

public class ServicoRequestDto
{
    public string Nome { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public decimal Preco { get; set; }
}
