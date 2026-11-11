using OficinaMecanica.Execucao.Application.Inputs.Execucoes;
using OficinaMecanica.Execucao.Application.UseCases.Execucoes;
using OficinaMecanica.Execucao.Domain.Entities;
using OficinaMecanica.Execucao.InterfaceAdapters.DTOs;
using OficinaMecanica.Execucao.InterfaceAdapters.Presenters;

namespace OficinaMecanica.Execucao.InterfaceAdapters.Controllers;

public class ExecucoesCleanController
{
    private readonly CriarExecucaoUseCase _criar;
    private readonly ObterExecucaoPorIdUseCase _obter;
    private readonly ObterFilaExecucoesUseCase _fila;
    private readonly IniciarDiagnosticoUseCase _iniciar;
    private readonly ConcluirDiagnosticoUseCase _concluir;
    private readonly IniciarReparoUseCase _iniciarReparo;

    public ExecucoesCleanController(CriarExecucaoUseCase criar, ObterExecucaoPorIdUseCase obter,
        ObterFilaExecucoesUseCase fila, IniciarDiagnosticoUseCase iniciar, ConcluirDiagnosticoUseCase concluir,
        IniciarReparoUseCase iniciarReparo)
    {
        _criar = criar;
        _obter = obter;
        _fila = fila;
        _iniciar = iniciar;
        _concluir = concluir;
        _iniciarReparo = iniciarReparo;
    }

    public ExecucaoResponseDto Criar(CriarExecucaoRequestDto request) => ExecucaoPresenter.Apresentar(
        _criar.Executar(new CriarExecucaoInput(request.OrdemServicoId, request.DescricaoSolicitacao)));
    public ExecucaoResponseDto? ObterPorId(Guid id)
    {
        var output = _obter.Executar(id);
        return output is null ? null : ExecucaoPresenter.Apresentar(output);
    }
    public List<ExecucaoResponseDto> ObterFila(StatusExecucao? status)
        => _fila.Executar(status).Select(ExecucaoPresenter.Apresentar).ToList();
    public ExecucaoResponseDto? IniciarDiagnostico(Guid id)
    {
        var output = _iniciar.Executar(id);
        return output is null ? null : ExecucaoPresenter.Apresentar(output);
    }
    public ExecucaoResponseDto? IniciarReparo(Guid id)
    {
        var output = _iniciarReparo.Executar(id);
        return output is null ? null : ExecucaoPresenter.Apresentar(output);
    }
    public ExecucaoResponseDto? ConcluirDiagnostico(Guid id, ConcluirDiagnosticoRequestDto request)
    {
        var output = _concluir.Executar(id, new ConcluirDiagnosticoInput
        {
            Diagnostico = request.Diagnostico,
            ServicoIds = request.ServicoIds,
            PecasInsumos = request.PecasInsumos.Select(item => new ItemPecaDiagnosticoInput
            {
                PecaInsumoId = item.PecaInsumoId,
                Quantidade = item.Quantidade
            }).ToList()
        });
        return output is null ? null : ExecucaoPresenter.Apresentar(output);
    }
}
