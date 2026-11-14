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
    public List<RespostaLiberacaoExecucao> RespostasLiberacao { get; set; } = [];
    public AutorizacaoExecucao? Autorizacao { get; set; }
    public DateTime? ReparoIniciadoEm { get; set; }
    public DateTime? ReparoConcluidoEm { get; set; }
    public List<ReservaPeca> ReservasPecas { get; set; } = [];
    public List<EventoReparo> EventosReparo { get; set; } = [];
    public List<EventoConclusaoReparo> EventosConclusaoReparo { get; set; } = [];
    public List<RespostaCancelamentoPago> RespostasCancelamentoPago { get; set; } = [];
    public DateTime? ReparoCanceladoEm { get; set; }
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

    public void LiberarExecucao(AutorizacaoExecucao autorizacao)
    {
        if (Status != StatusExecucao.AguardandoLiberacao || !DiagnosticoConcluidoEm.HasValue
            || !EntradaRegistrada || !SagaId.HasValue || Autorizacao is not null)
            throw new RegraNegocioException("A execucao nao esta aguardando sua primeira liberacao.");
        if (autorizacao is null || autorizacao.OrcamentoId == Guid.Empty || autorizacao.PagamentoId == Guid.Empty
            || autorizacao.MensagemLiberacaoId == Guid.Empty || autorizacao.ConfirmacaoPagamentoId == Guid.Empty
            || string.IsNullOrWhiteSpace(autorizacao.ProvedorId) || autorizacao.ProvedorId.Length > 100
            || autorizacao.ProvedorId.Any(char.IsControl) || autorizacao.ValorPago <= 0
            || autorizacao.ValorPago != Servicos.Sum(item => item.Preco) + PecasInsumos.Sum(item => item.Quantidade * item.PrecoUnitario))
            throw new RegraNegocioException("A autorizacao financeira diverge do diagnostico.");
        autorizacao.LiberadaEm = DateTime.UtcNow;
        Autorizacao = autorizacao;
        RegistrarStatus(StatusExecucao.AguardandoExecucao, autorizacao.LiberadaEm);
    }

    public bool IniciarReparo()
    {
        if (Status == StatusExecucao.Cancelada)
            throw new RegraNegocioException("Um reparo cancelado nao pode ser reiniciado.");
        if (ReparoIniciadoEm.HasValue) return false;
        var liberacao = RespostasLiberacao.SingleOrDefault(item => item.Tipo == "ExecucaoLiberada"
            && item.CausationId == Autorizacao?.MensagemLiberacaoId);
        if (Status != StatusExecucao.AguardandoExecucao || Autorizacao is null || !SagaId.HasValue
            || !EntradaRegistrada || !DiagnosticoConcluidoEm.HasValue || liberacao is null
            || liberacao.PagamentoId != Autorizacao.PagamentoId || liberacao.OrcamentoId != Autorizacao.OrcamentoId
            || liberacao.SagaId != SagaId || ReservasPecas.Count != 0
            || PecasInsumos.Any(item => item.PecaInsumoId == Guid.Empty || item.Quantidade <= 0)
            || PecasInsumos.Select(item => item.PecaInsumoId).Distinct().Count() != PecasInsumos.Count)
            throw new RegraNegocioException("O reparo exige uma execucao liberada pela Saga e ainda nao iniciada.");
        var agora = DateTime.UtcNow;
        ReservasPecas = PecasInsumos.Select(item => new ReservaPeca
        { PecaInsumoId = item.PecaInsumoId, Quantidade = item.Quantidade, ReservadaEm = agora }).ToList();
        ReparoIniciadoEm = agora;
        RegistrarStatus(StatusExecucao.EmExecucao, agora);
        EventosReparo.Add(new EventoReparo
        {
            SagaId = SagaId.Value,
            CausationId = Autorizacao.MensagemLiberacaoId,
            CorrelationId = liberacao.CorrelationId,
            OrdemServicoId = OrdemServicoId,
            ExecucaoId = Id,
            OrcamentoId = Autorizacao.OrcamentoId,
            PagamentoId = Autorizacao.PagamentoId,
            ProvedorId = Autorizacao.ProvedorId,
            ValorPago = Autorizacao.ValorPago,
            IniciadoEm = agora
        });
        return true;
    }

    public bool ConcluirReparo()
    {
        if (ReparoConcluidoEm.HasValue) return false;
        var inicio = EventosReparo.SingleOrDefault();
        if (Status != StatusExecucao.EmExecucao || !ReparoIniciadoEm.HasValue || Autorizacao is null
            || !SagaId.HasValue || inicio is null || inicio.SagaId != SagaId
            || inicio.PagamentoId != Autorizacao.PagamentoId || inicio.OrcamentoId != Autorizacao.OrcamentoId
            || inicio.CausationId != Autorizacao.MensagemLiberacaoId || inicio.IniciadoEm != ReparoIniciadoEm
            || ReservasPecas.Any(item => item.PecaInsumoId == Guid.Empty || item.Quantidade <= 0 || item.ConsumidaEm.HasValue || item.LiberadaEm.HasValue)
            || !ReservasPecas.OrderBy(item => item.PecaInsumoId).Select(item => (item.PecaInsumoId, item.Quantidade))
                .SequenceEqual(PecasInsumos.OrderBy(item => item.PecaInsumoId).Select(item => (item.PecaInsumoId, item.Quantidade)))
            || ReservasPecas.Select(item => item.PecaInsumoId).Distinct().Count() != ReservasPecas.Count)
            throw new RegraNegocioException("O reparo exige inicio autorizado e reservas integras para conclusao.");
        var agora = DateTime.UtcNow;
        if (agora < ReparoIniciadoEm.Value)
            throw new RegraNegocioException("A conclusao nao pode ocorrer antes do inicio do reparo.");
        foreach (var reserva in ReservasPecas) reserva.ConsumidaEm = agora;
        ReparoConcluidoEm = agora;
        RegistrarStatus(StatusExecucao.Finalizada, agora);
        EventosConclusaoReparo.Add(new EventoConclusaoReparo
        {
            SagaId = SagaId.Value,
            CausationId = Autorizacao.MensagemLiberacaoId,
            CorrelationId = inicio.CorrelationId,
            OrdemServicoId = OrdemServicoId,
            ExecucaoId = Id,
            OrcamentoId = Autorizacao.OrcamentoId,
            PagamentoId = Autorizacao.PagamentoId,
            ProvedorId = Autorizacao.ProvedorId,
            ValorPago = Autorizacao.ValorPago,
            IniciadoEm = ReparoIniciadoEm.Value,
            ConcluidoEm = agora
        });
        return true;
    }

    public void CancelarReparoPago()
    {
        if (Status is not (StatusExecucao.AguardandoLiberacao or StatusExecucao.AguardandoExecucao or StatusExecucao.EmExecucao)
            || ReparoConcluidoEm.HasValue || !DiagnosticoConcluidoEm.HasValue || !EntradaRegistrada || !SagaId.HasValue)
            throw new RegraNegocioException("Reparo concluido, cancelado ou sem diagnostico nao permite esta compensacao.");
        if (Status == StatusExecucao.EmExecucao)
        {
            if (!ReparoIniciadoEm.HasValue || Autorizacao is null
                || ReservasPecas.Any(item => item.ConsumidaEm.HasValue || item.LiberadaEm.HasValue || item.Quantidade <= 0)
                || ReservasPecas.Select(item => item.PecaInsumoId).Distinct().Count() != ReservasPecas.Count
                || !ReservasPecas.OrderBy(item => item.PecaInsumoId).Select(item => (item.PecaInsumoId, item.Quantidade))
                    .SequenceEqual(PecasInsumos.OrderBy(item => item.PecaInsumoId).Select(item => (item.PecaInsumoId, item.Quantidade))))
                throw new RegraNegocioException("Reservas do reparo inconsistentes; cancelamento requer verificacao.");
        }
        else if (ReservasPecas.Count != 0 || ReparoIniciadoEm.HasValue)
            throw new RegraNegocioException("Uma execucao ainda nao iniciada nao deve possuir reservas.");
        var agora = DateTime.UtcNow;
        foreach (var reserva in ReservasPecas) reserva.LiberadaEm = agora;
        ReparoCanceladoEm = agora;
        RegistrarStatus(StatusExecucao.Cancelada, agora);
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
