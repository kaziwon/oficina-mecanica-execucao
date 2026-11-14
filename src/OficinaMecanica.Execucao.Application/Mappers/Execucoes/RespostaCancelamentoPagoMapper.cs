using OficinaMecanica.Execucao.Application.Messaging;
using OficinaMecanica.Execucao.Domain.Entities;
namespace OficinaMecanica.Execucao.Application.Mappers.Execucoes;

public static class RespostaCancelamentoPagoMapper
{
    public static MensagemIntegracao Mapear(RespostaCancelamentoPago resposta) => new()
    {
        MessageId = resposta.MessageId,
        Type = resposta.Aceito ? "ReparoPagoCancelado" : "CancelamentoReparoPagoRecusado",
        SagaId = resposta.SagaId,
        CausationId = resposta.CausationId,
        CorrelationId = resposta.CorrelationId,
        OccurredAt = resposta.CriadaEm,
        Payload = new DadosMensagem
        {
            OrdemServicoId = resposta.OrdemServicoId,
            ExecucaoId = resposta.ExecucaoId,
            OrcamentoId = resposta.OrcamentoId,
            PagamentoId = resposta.PagamentoId,
            ProvedorId = resposta.ProvedorId,
            ValorTotal = resposta.ValorPago,
            Moeda = "BRL",
            Codigo = resposta.Aceito ? null : "ReparoNaoCancelavel",
            Motivo = resposta.Motivo
        }
    };
}
