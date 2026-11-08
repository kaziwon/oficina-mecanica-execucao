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
    public Guid? SagaId { get; set; }
    public Guid? PedidoRegistroId { get; set; }
    public bool EntradaRegistrada { get; set; } = true;
    public List<RespostaEntrada> RespostasEntrada { get; set; } = [];
    public List<EventoDiagnostico> EventosDiagnostico { get; set; } = [];
    public List<RespostaRecusaOrcamento> RespostasRecusaOrcamento { get; set; } = [];
    public List<ItemServicoDiagnostico> Servicos { get; set; } = [];
    public List<ItemPecaDiagnostico> PecasInsumos { get; set; } = [];
    public List<HistoricoExecucao> Historico { get; set; } = [];

    public void IniciarDiagnostico()
    {
        if (Status != StatusExecucao.AguardandoDiagnostico)
            throw new RegraNegocioException("A execucao nao esta aguardando diagnostico.");
        DiagnosticoIniciadoEm = DateTime.UtcNow;
        RegistrarStatus(StatusExecucao.EmDiagnostico, DiagnosticoIniciadoEm.Value);
        RegistrarEventoDiagnostico("DiagnosticoIniciado", DiagnosticoIniciadoEm.Value);
    }

    public void CancelarEntrada()
    {
        if (Status == StatusExecucao.Cancelada) return;
        if (Status != StatusExecucao.AguardandoDiagnostico)
            throw new RegraNegocioException("O diagnostico ja iniciou; esta compensacao exige tratamento explicito.");
        RegistrarStatus(StatusExecucao.Cancelada, DateTime.UtcNow);
    }

    public void CancelarPorRecusaOrcamento()
    {
        if (Status != StatusExecucao.AguardandoLiberacao || !DiagnosticoConcluidoEm.HasValue)
            throw new RegraNegocioException("Somente uma execucao diagnosticada, ainda nao liberada, pode ser cancelada por recusa.");
        RegistrarStatus(StatusExecucao.Cancelada, DateTime.UtcNow);
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
        RegistrarEventoDiagnostico("DiagnosticoConcluido", DiagnosticoConcluidoEm.Value);
    }

    private void RegistrarEventoDiagnostico(string tipo, DateTime agora)
    {
        if (!SagaId.HasValue) return;
        var entrada = RespostasEntrada.Single(item => item.SagaId == SagaId
            && item.CausationId == PedidoRegistroId && item.Tipo == "ExecucaoRegistrada");
        var concluido = tipo == "DiagnosticoConcluido";
        EventosDiagnostico.Add(new EventoDiagnostico
        {
            Tipo = tipo,
            SagaId = SagaId.Value,
            CausationId = PedidoRegistroId!.Value,
            OrdemServicoId = OrdemServicoId,
            ExecucaoId = Id,
            CorrelationId = entrada.CorrelationId,
            CriadoEm = agora,
            IniciadoEm = DiagnosticoIniciadoEm!.Value,
            ConcluidoEm = concluido ? DiagnosticoConcluidoEm : null,
            Diagnostico = concluido ? Diagnostico : null,
            Servicos = concluido ? Servicos.Select(item => new ItemServicoDiagnostico
            { ServicoId = item.ServicoId, Nome = item.Nome, Preco = item.Preco }).ToList() : [],
            PecasInsumos = concluido ? PecasInsumos.Select(item => new ItemPecaDiagnostico
            {
                PecaInsumoId = item.PecaInsumoId,
                Nome = item.Nome,
                Quantidade = item.Quantidade,
                PrecoUnitario = item.PrecoUnitario
            }).ToList() : []
        });
    }

    private void RegistrarStatus(StatusExecucao status, DateTime agora)
    {
        Status = status;
        Historico.Add(new HistoricoExecucao { Status = status, RegistradoEm = agora });
        Versao++;
    }
}
