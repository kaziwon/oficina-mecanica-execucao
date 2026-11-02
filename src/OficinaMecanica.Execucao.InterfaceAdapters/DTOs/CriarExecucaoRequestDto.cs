namespace OficinaMecanica.Execucao.InterfaceAdapters.DTOs;

public class CriarExecucaoRequestDto
{
    public Guid OrdemServicoId { get; set; }
    public string DescricaoSolicitacao { get; set; } = string.Empty;
}
