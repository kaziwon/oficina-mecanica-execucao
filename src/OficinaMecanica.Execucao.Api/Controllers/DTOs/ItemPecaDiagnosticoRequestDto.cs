using System.ComponentModel.DataAnnotations;

namespace OficinaMecanica.Execucao.Controllers.DTOs;

public class ItemPecaDiagnosticoRequestDto
{
    public Guid PecaInsumoId { get; set; }
    [Range(1, int.MaxValue)]
    public int Quantidade { get; set; }
}
