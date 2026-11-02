using OficinaMecanica.Execucao.Application.Inputs.PecasInsumos;
using OficinaMecanica.Execucao.Application.Outputs.PecasInsumos;
using OficinaMecanica.Execucao.Application.Mappers.PecasInsumos;
using OficinaMecanica.Execucao.Application.Gateways;
using OficinaMecanica.Execucao.Domain.Entities;
using OficinaMecanica.Execucao.Application.Exceptions;
using OficinaMecanica.Execucao.Application.Validators;

namespace OficinaMecanica.Execucao.Application.UseCases.PecasInsumos;

public class AtualizarPecaInsumoUseCase
{
    private const string NomeJaCadastrado = "Ja existe uma peca ou insumo cadastrado com este nome.";
    private readonly IPecaInsumoGateway _pecaInsumoGateway;

    public AtualizarPecaInsumoUseCase(IPecaInsumoGateway pecaInsumoGateway)
    {
        _pecaInsumoGateway = pecaInsumoGateway;
    }

    public PecaInsumoOutput? Executar(Guid id, AtualizarPecaInsumoInput input)
    {
        CatalogoValidator.Validar(input.Nome, input.Descricao, input.PrecoUnitario);
        if (_pecaInsumoGateway.ExistePorNomeExcetoId(input.Nome, id))
        {
            throw new ConflitoException(NomeJaCadastrado);
        }

        var pecaInsumo = new PecaInsumo
        {
            Id = id,
            Nome = input.Nome.Trim(),
            Descricao = input.Descricao,
            PrecoUnitario = input.PrecoUnitario
        };

        var pecaInsumoAtualizada = _pecaInsumoGateway.Atualizar(pecaInsumo);

        return pecaInsumoAtualizada is null ? null : PecaInsumoOutputMapper.Mapear(pecaInsumoAtualizada);
    }
}
