using OficinaMecanica.Execucao.Application.Messaging;
using OficinaMecanica.Execucao.Domain.Entities;

namespace OficinaMecanica.Execucao.Application.Mappers.Execucoes;

public static class EventoConclusaoReparoMapper
{
    public static MensagemIntegracao Mapear(EventoConclusaoReparo evento) => new()
    {
        MessageId = evento.MessageId,
        Type = "ReparoConcluido",
        SagaId = evento.SagaId,
        CausationId = evento.CausationId,
        CorrelationId = evento.CorrelationId,
        OccurredAt = evento.ConcluidoEm,
        Payload = new DadosMensagem
        {
            OrdemServicoId = evento.OrdemServicoId,
            ExecucaoId = evento.ExecucaoId,
            OrcamentoId = evento.OrcamentoId,
            PagamentoId = evento.PagamentoId,
            ProvedorId = evento.ProvedorId,
            ValorTotal = evento.ValorPago,
            Moeda = "BRL",
            ReparoIniciadoEm = evento.IniciadoEm,
            ReparoConcluidoEm = evento.ConcluidoEm
        }
    };
}
