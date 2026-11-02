using OficinaMecanica.Execucao.Application.Inputs.Servicos;
using OficinaMecanica.Execucao.Application.Outputs.Servicos;
using OficinaMecanica.Execucao.Domain.Entities;

namespace OficinaMecanica.Execucao.Application.Mappers.Servicos;

internal static class ServicoOutputMapper
{
    public static ServicoOutput Mapear(Servico servico)
    {
        return new ServicoOutput
        {
            Id = servico.Id,
            Nome = servico.Nome,
            Descricao = servico.Descricao,
            Preco = servico.Preco
        };
    }
}
