using OficinaMecanica.Execucao.Application.Exceptions;
using OficinaMecanica.Execucao.Application.Gateways;
using OficinaMecanica.Execucao.Application.Messaging;
using OficinaMecanica.Execucao.Domain.Entities;

namespace OficinaMecanica.Execucao.Application.UseCases.Execucoes;

public class ProcessarComandoEntradaUseCase
{
    private readonly IEntradaExecucaoGateway _entradas;
    public ProcessarComandoEntradaUseCase(IEntradaExecucaoGateway entradas) { _entradas = entradas; }

    public void Executar(MensagemIntegracao mensagem)
    {
        MensagemValidator.Validar(mensagem, "RegistrarExecucao", "CancelarEntradaExecucao");
        var registrar = mensagem.Type == "RegistrarExecucao";
        if (mensagem.Payload.ExecucaoId.HasValue || mensagem.Payload.Codigo is not null
            || (registrar && (string.IsNullOrWhiteSpace(mensagem.Payload.DescricaoSolicitacao)
                || mensagem.CausationId.HasValue || mensagem.Payload.Motivo is not null))
            || (!registrar && (!mensagem.CausationId.HasValue || string.IsNullOrWhiteSpace(mensagem.Payload.Motivo)
                || mensagem.Payload.DescricaoSolicitacao is not null)))
            throw new ValidacaoException("Campos invalidos para o comando de entrada.");

        for (var tentativa = 0; tentativa < 5; tentativa++)
        {
            var execucao = _entradas.ObterPorOrdemServicoId(mensagem.Payload.OrdemServicoId);
            var nova = execucao is null;
            execucao ??= new ExecucaoServico
            {
                Id = Guid.NewGuid(),
                OrdemServicoId = mensagem.Payload.OrdemServicoId,
                SagaId = mensagem.SagaId,
                PedidoRegistroId = registrar ? mensagem.MessageId : mensagem.CausationId,
                DescricaoSolicitacao = registrar ? mensagem.Payload.DescricaoSolicitacao!.Trim() : "Entrada cancelada antes do registro",
                Status = StatusExecucao.AguardandoDiagnostico,
                CriadaEm = DateTime.UtcNow,
                EntradaRegistrada = registrar
            };
            var repetida = execucao.RespostasEntrada.SingleOrDefault(item => item.CausationId == mensagem.MessageId);
            if (repetida is not null)
            {
                if (repetida.SagaId != mensagem.SagaId || repetida.TipoComando != mensagem.Type
                    || repetida.DescricaoComando != mensagem.Payload.DescricaoSolicitacao?.Trim()
                    || repetida.MotivoComando != mensagem.Payload.Motivo?.Trim() || repetida.CausaComando != mensagem.CausationId
                    || repetida.CorrelationId != mensagem.CorrelationId)
                    throw new ValidacaoException("O messageId foi reutilizado com outros dados.");
                return;
            }
            var versao = execucao.Versao;
            var resposta = new RespostaEntrada
            {
                SagaId = mensagem.SagaId,
                CausationId = mensagem.MessageId,
                CausaComando = mensagem.CausationId,
                CorrelationId = mensagem.CorrelationId,
                OrdemServicoId = mensagem.Payload.OrdemServicoId,
                TipoComando = mensagem.Type,
                DescricaoComando = mensagem.Payload.DescricaoSolicitacao?.Trim(),
                MotivoComando = mensagem.Payload.Motivo?.Trim()
            };

            if (registrar) Registrar(execucao, mensagem, resposta, nova);
            else Cancelar(execucao, mensagem, resposta);

            execucao.RespostasEntrada.Add(resposta);
            execucao.Versao++;
            if (nova ? _entradas.TentarInserir(execucao) : _entradas.TentarAtualizar(execucao, versao)) return;
        }
        throw new ConflitoException("A entrada mudou durante o processamento; a mensagem precisa ser tentada novamente.");
    }

    private static void Registrar(ExecucaoServico execucao, MensagemIntegracao mensagem, RespostaEntrada resposta, bool nova)
    {
        if (execucao.SagaId.HasValue && execucao.SagaId != mensagem.SagaId)
        {
            Recusar(resposta, "RegistroExecucaoRecusado", "ConflitoDeVinculo", "A entrada pertence a outro processo."); return;
        }
        if (execucao.Status == StatusExecucao.Cancelada)
        {
            Recusar(resposta, "RegistroExecucaoRecusado", "ProcessoCancelado", "A entrada ja foi cancelada."); return;
        }
        if (execucao.DescricaoSolicitacao != mensagem.Payload.DescricaoSolicitacao!.Trim()
            || (execucao.PedidoRegistroId.HasValue && execucao.PedidoRegistroId != mensagem.MessageId)
            || (!execucao.SagaId.HasValue && execucao.Status != StatusExecucao.AguardandoDiagnostico))
        {
            Recusar(resposta, "RegistroExecucaoRecusado", "ConflitoDeVinculo", "A OS ja possui uma entrada com dados ou andamento diferentes."); return;
        }
        execucao.SagaId = mensagem.SagaId;
        execucao.PedidoRegistroId = mensagem.MessageId;
        execucao.EntradaRegistrada = true;
        if (nova) execucao.Historico.Add(new HistoricoExecucao
        {
            Status = StatusExecucao.AguardandoDiagnostico,
            RegistradoEm = execucao.CriadaEm
        });
        resposta.Tipo = "ExecucaoRegistrada";
        resposta.ExecucaoId = execucao.Id;
    }

    private static void Cancelar(ExecucaoServico execucao, MensagemIntegracao mensagem, RespostaEntrada resposta)
    {
        if (execucao.SagaId != mensagem.SagaId || execucao.PedidoRegistroId != mensagem.CausationId)
        {
            Recusar(resposta, "CancelamentoEntradaExecucaoRecusado", "ConflitoDeVinculo", "O cancelamento nao corresponde ao processo desta entrada."); return;
        }
        if (execucao.Status is not (StatusExecucao.AguardandoDiagnostico or StatusExecucao.Cancelada))
        {
            Recusar(resposta, "CancelamentoEntradaExecucaoRecusado", "DiagnosticoJaIniciado", "O diagnostico ja iniciou e exige tratamento explicito."); return;
        }
        execucao.CancelarEntrada();
        resposta.Tipo = "EntradaExecucaoCancelada";
        resposta.ExecucaoId = execucao.EntradaRegistrada ? execucao.Id : null;
    }

    private static void Recusar(RespostaEntrada resposta, string tipo, string codigo, string motivo)
    {
        resposta.Tipo = tipo; resposta.Codigo = codigo; resposta.Motivo = motivo;
    }
}
