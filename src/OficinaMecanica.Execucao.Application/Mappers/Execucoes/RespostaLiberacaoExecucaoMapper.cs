using OficinaMecanica.Execucao.Application.Messaging;
using OficinaMecanica.Execucao.Domain.Entities;

namespace OficinaMecanica.Execucao.Application.Mappers.Execucoes;

public static class RespostaLiberacaoExecucaoMapper
{
    public static MensagemIntegracao Mapear(RespostaLiberacaoExecucao resposta) => new()
    {
        MessageId = resposta.MessageId,
        Type = resposta.Tipo,
        SagaId = resposta.SagaId,
        CausationId = resposta.CausationId,
        OccurredAt = resposta.CriadaEm,
        CorrelationId = resposta.CorrelationId,
        Payload = new DadosMensagem
        {
            OrdemServicoId = resposta.OrdemServicoId,
            ExecucaoId = resposta.ExecucaoId,
            OrcamentoId = resposta.OrcamentoId,
            PagamentoId = resposta.PagamentoId,
            ProvedorId = resposta.ProvedorId,
            ValorTotal = resposta.ValorPago,
            Moeda = resposta.Moeda,
            Codigo = resposta.Codigo,
            Motivo = resposta.Motivo
        }
    };
}
