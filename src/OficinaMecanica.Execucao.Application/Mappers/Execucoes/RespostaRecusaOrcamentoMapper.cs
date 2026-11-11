using OficinaMecanica.Execucao.Application.Messaging;
using OficinaMecanica.Execucao.Domain.Entities;

namespace OficinaMecanica.Execucao.Application.Mappers.Execucoes;

public static class RespostaRecusaOrcamentoMapper
{
    public static MensagemIntegracao Mapear(RespostaRecusaOrcamento resposta) => new()
    {
        MessageId = resposta.MessageId,
        Type = resposta.Tipo,
        SagaId = resposta.SagaId,
        CausationId = resposta.CausationId,
        CorrelationId = resposta.CorrelationId,
        OccurredAt = resposta.CriadaEm,
        Payload = new DadosMensagem
        {
            OrdemServicoId = resposta.OrdemServicoId,
            ExecucaoId = resposta.ExecucaoId,
            OrcamentoId = resposta.OrcamentoId,
            Codigo = resposta.Codigo,
            Motivo = resposta.Motivo
        }
    };
}
