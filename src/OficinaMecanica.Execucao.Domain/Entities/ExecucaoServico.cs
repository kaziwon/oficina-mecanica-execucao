using OficinaMecanica.Execucao.Domain.Exceptions;

namespace OficinaMecanica.Execucao.Domain.Entities;

public class ExecucaoServico
{
    public Guid Id { get; set; }
    public Guid OrdemServicoId { get; set; }
    public string DescricaoSolicitacao { get; set; } = string.Empty;
    public string Diagnostico { get; set; } = string.Empty;
    public StatusExecucao Status { get; set; }
    public DateTime CriadaEm { get; set; }
    public DateTime? DiagnosticoIniciadoEm { get; set; }
    public DateTime? DiagnosticoConcluidoEm { get; set; }
    public long Versao { get; set; }
    public List<ItemServicoDiagnostico> Servicos { get; set; } = [];
    public List<ItemPecaDiagnostico> PecasInsumos { get; set; } = [];
    public List<HistoricoExecucao> Historico { get; set; } = [];

    public void IniciarDiagnostico()
    {
        if (Status != StatusExecucao.AguardandoDiagnostico)
            throw new RegraNegocioException("A execucao nao esta aguardando diagnostico.");
        DiagnosticoIniciadoEm = DateTime.UtcNow;
        RegistrarStatus(StatusExecucao.EmDiagnostico, DiagnosticoIniciadoEm.Value);
    }

    public void ConcluirDiagnostico(string descricao, List<ItemServicoDiagnostico> servicos, List<ItemPecaDiagnostico> pecas)
    {
        if (Status != StatusExecucao.EmDiagnostico)
            throw new RegraNegocioException("A execucao nao esta em diagnostico.");
        Diagnostico = descricao.Trim();
        Servicos = servicos;
        PecasInsumos = pecas;
        DiagnosticoConcluidoEm = DateTime.UtcNow;
        RegistrarStatus(StatusExecucao.AguardandoLiberacao, DiagnosticoConcluidoEm.Value);
    }

    private void RegistrarStatus(StatusExecucao status, DateTime agora)
    {
        Status = status;
        Historico.Add(new HistoricoExecucao { Status = status, RegistradoEm = agora });
        Versao++;
    }
}
