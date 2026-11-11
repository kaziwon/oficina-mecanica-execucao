using OficinaMecanica.Execucao.Application.Exceptions;
using OficinaMecanica.Execucao.Application.Gateways;
using OficinaMecanica.Execucao.Application.Messaging;
using OficinaMecanica.Execucao.Domain.Entities;
using OficinaMecanica.Execucao.Domain.Exceptions;

namespace OficinaMecanica.Execucao.Application.UseCases.Execucoes;

public class ProcessarLiberacaoExecucaoUseCase
{
    private readonly IEntradaExecucaoGateway _execucoes;
    public ProcessarLiberacaoExecucaoUseCase(IEntradaExecucaoGateway execucoes) { _execucoes = execucoes; }

    public void Executar(MensagemIntegracao mensagem)
    {
        MensagemValidator.Validar(mensagem, "LiberarExecucao");
        var dados = mensagem.Payload;
        if (!mensagem.CausationId.HasValue || !dados.ExecucaoId.HasValue || !dados.OrcamentoId.HasValue || !dados.PagamentoId.HasValue
            || string.IsNullOrWhiteSpace(dados.ProvedorId) || dados.ProvedorId.Any(char.IsControl)
            || !dados.ValorTotal.HasValue || dados.ValorTotal <= 0 || dados.ValorTotal > 999999999.99m
            || decimal.Round(dados.ValorTotal.Value, 2) != dados.ValorTotal || dados.Moeda != "BRL"
            || dados.Codigo is not null || dados.Motivo is not null || dados.DescricaoSolicitacao is not null
            || dados.Diagnostico is not null || dados.Itens is not null || dados.DiagnosticoIniciadoEm.HasValue || dados.DiagnosticoConcluidoEm.HasValue)
            throw new ValidacaoException("Contrato de liberacao de execucao invalido.");
        for (var tentativa = 0; tentativa < 5; tentativa++)
        {
            var execucao = _execucoes.ObterPorOrdemServicoId(dados.OrdemServicoId)
                ?? throw new ConflitoException("Execucao ainda nao encontrada; o comando nao foi confirmado.");
            var entrada = execucao.RespostasEntrada.SingleOrDefault(item => item.Tipo == "ExecucaoRegistrada"
                && item.CausationId == execucao.PedidoRegistroId);
            if (execucao.Id != dados.ExecucaoId || execucao.SagaId != mensagem.SagaId || entrada is null
                || entrada.SagaId != mensagem.SagaId || entrada.CorrelationId != mensagem.CorrelationId || !execucao.EntradaRegistrada)
                throw new ValidacaoException("A liberacao nao pertence ao processo desta execucao.");
            if (execucao.RespostasEntrada.Any(item => item.CausationId == mensagem.MessageId)
                || execucao.RespostasRecusaOrcamento.Any(item => item.CausationId == mensagem.MessageId))
                throw new ValidacaoException("MessageId ja utilizado por outro comando.");
            var repetida = execucao.RespostasLiberacao.SingleOrDefault(item => item.CausationId == mensagem.MessageId);
            if (repetida is not null)
            {
                if (repetida.CausaComando != mensagem.CausationId || repetida.OrcamentoId != dados.OrcamentoId
                    || repetida.PagamentoId != dados.PagamentoId || repetida.ProvedorId != dados.ProvedorId
                    || repetida.ValorPago != dados.ValorTotal || repetida.Moeda != dados.Moeda)
                    throw new ValidacaoException("MessageId reutilizado com outros dados de liberacao.");
                return;
            }
            var versao = execucao.Versao;
            var resposta = new RespostaLiberacaoExecucao
            {
                SagaId = mensagem.SagaId,
                CausationId = mensagem.MessageId,
                CausaComando = mensagem.CausationId.Value,
                CorrelationId = mensagem.CorrelationId,
                OrdemServicoId = dados.OrdemServicoId,
                ExecucaoId = execucao.Id,
                OrcamentoId = dados.OrcamentoId.Value,
                PagamentoId = dados.PagamentoId.Value,
                ProvedorId = dados.ProvedorId,
                ValorPago = dados.ValorTotal.Value,
                Moeda = dados.Moeda
            };
            try
            {
                execucao.LiberarExecucao(new AutorizacaoExecucao
                {
                    OrcamentoId = resposta.OrcamentoId,
                    PagamentoId = resposta.PagamentoId,
                    ProvedorId = resposta.ProvedorId,
                    ValorPago = resposta.ValorPago,
                    MensagemLiberacaoId = mensagem.MessageId,
                    ConfirmacaoPagamentoId = mensagem.CausationId.Value
                });
                resposta.Tipo = "ExecucaoLiberada"; resposta.CriadaEm = execucao.Autorizacao!.LiberadaEm;
            }
            catch (RegraNegocioException exception)
            { resposta.Tipo = "LiberacaoExecucaoRecusada"; resposta.Codigo = "ExecucaoNaoLiberavel"; resposta.Motivo = exception.Message; }
            execucao.RespostasLiberacao.Add(resposta);
            execucao.Versao++;
            if (_execucoes.TentarAtualizar(execucao, versao)) return;
        }
        throw new ConflitoException("Execucao alterada durante a liberacao; tente a mensagem novamente.");
    }
}
