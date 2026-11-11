using OficinaMecanica.Execucao.Application.Exceptions;
using OficinaMecanica.Execucao.Application.Gateways;
using OficinaMecanica.Execucao.Application.Messaging;
using OficinaMecanica.Execucao.Domain.Entities;
using OficinaMecanica.Execucao.Domain.Exceptions;

namespace OficinaMecanica.Execucao.Application.UseCases.Execucoes;

public class ProcessarRecusaOrcamentoUseCase
{
    private readonly IEntradaExecucaoGateway _execucoes;
    public ProcessarRecusaOrcamentoUseCase(IEntradaExecucaoGateway execucoes) { _execucoes = execucoes; }

    public void Executar(MensagemIntegracao mensagem)
    {
        MensagemValidator.Validar(mensagem, "CancelarExecucaoPorRecusaOrcamento");
        var dados = mensagem.Payload;
        if (!mensagem.CausationId.HasValue || !dados.ExecucaoId.HasValue || !dados.OrcamentoId.HasValue
            || string.IsNullOrWhiteSpace(dados.Motivo) || dados.Codigo is not null || dados.ValorTotal.HasValue
            || dados.DescricaoSolicitacao is not null || dados.Diagnostico is not null || dados.Itens is not null
            || dados.DiagnosticoIniciadoEm.HasValue || dados.DiagnosticoConcluidoEm.HasValue)
            throw new ValidacaoException("Contrato do cancelamento por recusa invalido.");
        for (var tentativa = 0; tentativa < 5; tentativa++)
        {
            var execucao = _execucoes.ObterPorOrdemServicoId(dados.OrdemServicoId)
                ?? throw new ConflitoException("Execucao ainda nao encontrada; o comando nao foi confirmado.");
            var entrada = execucao.RespostasEntrada.SingleOrDefault(item => item.Tipo == "ExecucaoRegistrada"
                && item.CausationId == execucao.PedidoRegistroId);
            if (execucao.Id != dados.ExecucaoId || execucao.SagaId != mensagem.SagaId
                || entrada?.CorrelationId != mensagem.CorrelationId || !execucao.EntradaRegistrada)
                throw new ValidacaoException("O comando nao pertence ao processo desta execucao.");
            var repetida = execucao.RespostasRecusaOrcamento.SingleOrDefault(item => item.CausationId == mensagem.MessageId);
            if (repetida is not null)
            {
                if (repetida.CausaComando != mensagem.CausationId || repetida.OrcamentoId != dados.OrcamentoId
                    || repetida.MotivoComando != dados.Motivo)
                    throw new ValidacaoException("MessageId reutilizado com outros dados de recusa.");
                return;
            }
            var versao = execucao.Versao;
            var resposta = new RespostaRecusaOrcamento
            {
                SagaId = mensagem.SagaId,
                CausationId = mensagem.MessageId,
                CausaComando = mensagem.CausationId.Value,
                CorrelationId = mensagem.CorrelationId,
                OrdemServicoId = dados.OrdemServicoId,
                ExecucaoId = execucao.Id,
                OrcamentoId = dados.OrcamentoId.Value,
                MotivoComando = dados.Motivo
            };
            try
            {
                execucao.CancelarPorRecusaOrcamento();
                resposta.Tipo = "ExecucaoCanceladaPorRecusaOrcamento";
            }
            catch (RegraNegocioException exception)
            {
                resposta.Tipo = "CancelamentoPorRecusaOrcamentoRecusado";
                resposta.Codigo = "ExecucaoNaoCancelavel"; resposta.Motivo = exception.Message;
            }
            execucao.RespostasRecusaOrcamento.Add(resposta);
            execucao.Versao++;
            if (_execucoes.TentarAtualizar(execucao, versao)) return;
        }
        throw new ConflitoException("Execucao alterada durante o cancelamento; tente a mensagem novamente.");
    }
}
