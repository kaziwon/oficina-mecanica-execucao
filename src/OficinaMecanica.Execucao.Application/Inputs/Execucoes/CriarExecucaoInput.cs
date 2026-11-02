namespace OficinaMecanica.Execucao.Application.Inputs.Execucoes;

public class CriarExecucaoInput
{
    public CriarExecucaoInput(Guid ordemServicoId, string descricaoSolicitacao)
    {
        OrdemServicoId = ordemServicoId;
        DescricaoSolicitacao = descricaoSolicitacao;
    }

    public Guid OrdemServicoId { get; }
    public string DescricaoSolicitacao { get; }
}
