using System.ComponentModel.DataAnnotations;

namespace OficinaMecanica.Execucao.Controllers.DTOs;

public class CriarExecucaoRequestDto
{
    public Guid OrdemServicoId { get; set; }
    [Required, MaxLength(1000)]
    public string DescricaoSolicitacao { get; set; } = string.Empty;
}
