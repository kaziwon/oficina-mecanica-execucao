using OficinaMecanica.Execucao.Application.Inputs.PecasInsumos;
using OficinaMecanica.Execucao.Application.Outputs.PecasInsumos;
using OficinaMecanica.Execucao.InterfaceAdapters.DTOs;
using OficinaMecanica.Execucao.Application.UseCases.PecasInsumos;
using OficinaMecanica.Execucao.InterfaceAdapters.Presenters;

namespace OficinaMecanica.Execucao.InterfaceAdapters.Controllers;

public class PecasCleanController
{
    private readonly ObterTodasPecasInsumosUseCase _obterTodasPecasInsumosUseCase;
    private readonly ObterPecaInsumoPorIdUseCase _obterPecaInsumoPorIdUseCase;
    private readonly CriarPecaInsumoUseCase _criarPecaInsumoUseCase;
    private readonly AtualizarPecaInsumoUseCase _atualizarPecaInsumoUseCase;
    private readonly RemoverPecaInsumoUseCase _removerPecaInsumoUseCase;
    private readonly MovimentarEstoqueUseCase _movimentarEstoqueUseCase;

    public PecasCleanController(
        ObterTodasPecasInsumosUseCase obterTodasPecasInsumosUseCase,
        ObterPecaInsumoPorIdUseCase obterPecaInsumoPorIdUseCase,
        CriarPecaInsumoUseCase criarPecaInsumoUseCase,
        AtualizarPecaInsumoUseCase atualizarPecaInsumoUseCase,
        RemoverPecaInsumoUseCase removerPecaInsumoUseCase,
        MovimentarEstoqueUseCase movimentarEstoqueUseCase)
    {
        _obterTodasPecasInsumosUseCase = obterTodasPecasInsumosUseCase;
        _obterPecaInsumoPorIdUseCase = obterPecaInsumoPorIdUseCase;
        _criarPecaInsumoUseCase = criarPecaInsumoUseCase;
        _atualizarPecaInsumoUseCase = atualizarPecaInsumoUseCase;
        _removerPecaInsumoUseCase = removerPecaInsumoUseCase;
        _movimentarEstoqueUseCase = movimentarEstoqueUseCase;
    }

    public List<PecaInsumoResponseDto> ObterTodas()
    {
        return _obterTodasPecasInsumosUseCase.Executar().Select(PecaInsumoPresenter.Apresentar).ToList();
    }

    public PecaInsumoResponseDto? ObterPorId(Guid id)
    {
        var pecaInsumo = _obterPecaInsumoPorIdUseCase.Executar(id);

        return pecaInsumo is null ? null : PecaInsumoPresenter.Apresentar(pecaInsumo);
    }

    public PecaInsumoResponseDto Criar(PecaInsumoRequestDto pecaInsumoRequestDto)
    {
        var input = new PecaInsumoInput(
            pecaInsumoRequestDto.Nome,
            pecaInsumoRequestDto.Descricao,
            pecaInsumoRequestDto.PrecoUnitario,
            pecaInsumoRequestDto.QuantidadeEstoque);

        return PecaInsumoPresenter.Apresentar(_criarPecaInsumoUseCase.Executar(input));
    }

    public PecaInsumoResponseDto? Atualizar(Guid id, AtualizarPecaRequestDto pecaInsumoRequestDto)
    {
        var input = new AtualizarPecaInsumoInput(
            pecaInsumoRequestDto.Nome,
            pecaInsumoRequestDto.Descricao,
            pecaInsumoRequestDto.PrecoUnitario);
        var pecaInsumo = _atualizarPecaInsumoUseCase.Executar(id, input);

        return pecaInsumo is null ? null : PecaInsumoPresenter.Apresentar(pecaInsumo);
    }

    public bool Remover(Guid id)
    {
        return _removerPecaInsumoUseCase.Executar(id);
    }

    public PecaInsumoResponseDto? MovimentarEstoque(Guid id, int quantidade)
    {
        var output = _movimentarEstoqueUseCase.Executar(id, quantidade);
        return output is null ? null : PecaInsumoPresenter.Apresentar(output);
    }
}
