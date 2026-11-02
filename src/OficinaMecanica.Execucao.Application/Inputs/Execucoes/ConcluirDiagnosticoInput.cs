namespace OficinaMecanica.Execucao.Application.Inputs.Execucoes;

public class ConcluirDiagnosticoInput
{
    public string Diagnostico { get; set; } = string.Empty;
    public List<Guid> ServicoIds { get; set; } = [];
    public List<ItemPecaDiagnosticoInput> PecasInsumos { get; set; } = [];
}
