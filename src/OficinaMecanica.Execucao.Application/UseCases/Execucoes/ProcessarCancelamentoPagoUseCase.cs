using OficinaMecanica.Execucao.Application.Exceptions;
using OficinaMecanica.Execucao.Application.Gateways;
using OficinaMecanica.Execucao.Application.Messaging;
using OficinaMecanica.Execucao.Domain.Entities;
using OficinaMecanica.Execucao.Domain.Exceptions;
namespace OficinaMecanica.Execucao.Application.UseCases.Execucoes;

public class ProcessarCancelamentoPagoUseCase
{
    private readonly IEntradaExecucaoGateway _entradas;
    private readonly ICancelamentoPagoGateway _cancelamento;
    public ProcessarCancelamentoPagoUseCase(IEntradaExecucaoGateway entradas, ICancelamentoPagoGateway cancelamento)
    { _entradas = entradas; _cancelamento = cancelamento; }
    public void Executar(MensagemIntegracao mensagem)
    {
        MensagemValidator.Validar(mensagem, "CancelarReparoPago");
        var dados = mensagem.Payload;
        if (string.IsNullOrWhiteSpace(dados.Motivo) || dados.Codigo is not null)
            throw new ValidacaoException("Informe o motivo do cancelamento pago.");
        for (var tentativa = 0; tentativa < 5; tentativa++)
        {
            var execucao = _entradas.ObterPorOrdemServicoId(dados.OrdemServicoId)
                ?? throw new ConflitoException("Execucao ainda nao encontrada; comando nao confirmado.");
            var entrada = execucao.RespostasEntrada.SingleOrDefault(item => item.Tipo == "ExecucaoRegistrada" && item.CausationId == execucao.PedidoRegistroId);
            if (execucao.Id != dados.ExecucaoId || execucao.SagaId != mensagem.SagaId || entrada?.CorrelationId != mensagem.CorrelationId
                || !execucao.EntradaRegistrada || dados.ValorTotal != execucao.Servicos.Sum(item => item.Preco)
                    + execucao.PecasInsumos.Sum(item => item.Quantidade * item.PrecoUnitario)
                || (execucao.Autorizacao is not null && (execucao.Autorizacao.MensagemLiberacaoId != mensagem.CausationId
                    || execucao.Autorizacao.PagamentoId != dados.PagamentoId || execucao.Autorizacao.OrcamentoId != dados.OrcamentoId
                    || execucao.Autorizacao.ProvedorId != dados.ProvedorId)))
                throw new ValidacaoException("Cancelamento nao pertence a autorizacao desta execucao.");
            var repetida = execucao.RespostasCancelamentoPago.SingleOrDefault(item => item.CausationId == mensagem.MessageId);
            if (repetida is not null)
            {
                if (repetida.CausaComando != mensagem.CausationId || repetida.PagamentoId != dados.PagamentoId
                    || repetida.OrcamentoId != dados.OrcamentoId || repetida.ProvedorId != dados.ProvedorId
                    || repetida.ValorPago != dados.ValorTotal || repetida.MotivoComando != dados.Motivo)
                    throw new ValidacaoException("MessageId reutilizado com outros dados de cancelamento.");
                return;
            }
            if (execucao.RespostasEntrada.Any(item => item.CausationId == mensagem.MessageId)
                || execucao.RespostasLiberacao.Any(item => item.CausationId == mensagem.MessageId)
                || execucao.RespostasRecusaOrcamento.Any(item => item.CausationId == mensagem.MessageId))
                throw new ValidacaoException("MessageId ja utilizado por outro comando.");
            var versao = execucao.Versao;
            try
            {
                execucao.CancelarReparoPago();
                var resposta = Resposta(mensagem); resposta.Aceito = true; resposta.CriadaEm = execucao.ReparoCanceladoEm!.Value;
                execucao.RespostasCancelamentoPago.Add(resposta); execucao.Versao++;
                if (_cancelamento.LiberarECancelar(execucao, versao)) return;
            }
            catch (RegraNegocioException exception)
            {
                // A transacao foi desfeita: a recusa deve partir de uma nova leitura, sem reservas liberadas em memoria.
                var salva = _entradas.ObterPorOrdemServicoId(dados.OrdemServicoId)!;
                if (salva.Versao != versao) continue;
                var recusa = Resposta(mensagem); recusa.Motivo = exception.Message;
                salva.RespostasCancelamentoPago.Add(recusa); salva.Versao++;
                if (_entradas.TentarAtualizar(salva, versao)) return;
            }
        }
        throw new ConflitoException("Execucao alterada durante a compensacao; reentregue o comando.");
    }

    private static RespostaCancelamentoPago Resposta(MensagemIntegracao mensagem) => new()
    {
        SagaId = mensagem.SagaId,
        CausationId = mensagem.MessageId,
        CausaComando = mensagem.CausationId!.Value,
        CorrelationId = mensagem.CorrelationId,
        OrdemServicoId = mensagem.Payload.OrdemServicoId,
        ExecucaoId = mensagem.Payload.ExecucaoId!.Value,
        OrcamentoId = mensagem.Payload.OrcamentoId!.Value,
        PagamentoId = mensagem.Payload.PagamentoId!.Value,
        ProvedorId = mensagem.Payload.ProvedorId!,
        ValorPago = mensagem.Payload.ValorTotal!.Value,
        MotivoComando = mensagem.Payload.Motivo!
    };
}
