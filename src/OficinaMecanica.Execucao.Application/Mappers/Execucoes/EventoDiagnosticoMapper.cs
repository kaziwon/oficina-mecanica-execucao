using OficinaMecanica.Execucao.Application.Messaging;
using OficinaMecanica.Execucao.Domain.Entities;

namespace OficinaMecanica.Execucao.Application.Mappers.Execucoes;

public static class EventoDiagnosticoMapper
{
    public static MensagemIntegracao Mapear(EventoDiagnostico evento) => new()
    {
        MessageId = evento.MessageId,
        Type = evento.Tipo,
        SagaId = evento.SagaId,
        CausationId = evento.CausationId,
        CorrelationId = evento.CorrelationId,
        OccurredAt = evento.CriadoEm,
        Payload = new DadosMensagem
        {
            OrdemServicoId = evento.OrdemServicoId,
            ExecucaoId = evento.ExecucaoId,
            DiagnosticoIniciadoEm = evento.IniciadoEm,
            DiagnosticoConcluidoEm = evento.ConcluidoEm,
            Diagnostico = evento.Diagnostico,
            Itens = evento.Tipo != "DiagnosticoConcluido" ? null : evento.Servicos.Select(item => new ItemMensagem
            {
                Tipo = "Servico",
                ReferenciaId = item.ServicoId,
                Nome = item.Nome,
                Quantidade = 1,
                PrecoUnitario = item.Preco
            }).Concat(evento.PecasInsumos.Select(item => new ItemMensagem
            {
                Tipo = "PecaInsumo",
                ReferenciaId = item.PecaInsumoId,
                Nome = item.Nome,
                Quantidade = item.Quantidade,
                PrecoUnitario = item.PrecoUnitario
            })).ToList()
        }
    };
}
