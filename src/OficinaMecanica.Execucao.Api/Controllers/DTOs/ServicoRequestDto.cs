using System.ComponentModel.DataAnnotations;

namespace OficinaMecanica.Execucao.Controllers.DTOs;

public class ServicoRequestDto
{
    [Required]
    [MaxLength(150)]
    public string Nome { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Descricao { get; set; } = string.Empty;

    [Range(0.01, 999999.99)]
    public decimal Preco { get; set; }
}
