using System.ComponentModel.DataAnnotations;

namespace OficinaMecanica.Execucao.Controllers.DTOs;

public class ConcluirDiagnosticoRequestDto : IValidatableObject
{
    [Required, MaxLength(2000)]
    public string Diagnostico { get; set; } = string.Empty;
    public List<Guid>? ServicoIds { get; set; } = [];
    public List<ItemPecaDiagnosticoRequestDto>? PecasInsumos { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (PecasInsumos?.Any(item => item is null) == true)
            yield return new ValidationResult("A lista de pecas nao pode conter itens nulos.", [nameof(PecasInsumos)]);
    }
}
