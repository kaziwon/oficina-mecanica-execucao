namespace OficinaMecanica.Execucao.InterfaceAdapters.DTOs;

public class ConcluirDiagnosticoRequestDto
{
    public string Diagnostico { get; set; } = string.Empty;
    public List<Guid> ServicoIds { get; set; } = [];
    public List<ItemPecaDiagnosticoRequestDto> PecasInsumos { get; set; } = [];
}
