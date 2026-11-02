using CleanDtos = OficinaMecanica.Execucao.InterfaceAdapters.DTOs;
using HttpDtos = OficinaMecanica.Execucao.Controllers.DTOs;

namespace OficinaMecanica.Execucao.Controllers.Mappers;

public static class HttpRequestDtoMapper
{
    public static CleanDtos.LoginRequestDto ParaCleanDto(this HttpDtos.LoginRequestDto dto)
        => new() { Username = dto.Username, Password = dto.Password };
    public static CleanDtos.ServicoRequestDto ParaCleanDto(this HttpDtos.ServicoRequestDto dto)
        => new() { Nome = dto.Nome, Descricao = dto.Descricao, Preco = dto.Preco };
    public static CleanDtos.PecaInsumoRequestDto ParaCleanDto(this HttpDtos.PecaInsumoRequestDto dto)
        => new() { Nome = dto.Nome, Descricao = dto.Descricao, PrecoUnitario = dto.PrecoUnitario, QuantidadeEstoque = dto.QuantidadeEstoque };
    public static CleanDtos.AtualizarPecaRequestDto ParaCleanDto(this HttpDtos.AtualizarPecaRequestDto dto)
        => new() { Nome = dto.Nome, Descricao = dto.Descricao, PrecoUnitario = dto.PrecoUnitario };
    public static CleanDtos.CriarExecucaoRequestDto ParaCleanDto(this HttpDtos.CriarExecucaoRequestDto dto)
        => new() { OrdemServicoId = dto.OrdemServicoId, DescricaoSolicitacao = dto.DescricaoSolicitacao };
    public static CleanDtos.ConcluirDiagnosticoRequestDto ParaCleanDto(this HttpDtos.ConcluirDiagnosticoRequestDto dto)
        => new()
        {
            Diagnostico = dto.Diagnostico,
            ServicoIds = dto.ServicoIds ?? [],
            PecasInsumos = (dto.PecasInsumos ?? []).Select(item => new CleanDtos.ItemPecaDiagnosticoRequestDto
            {
                PecaInsumoId = item.PecaInsumoId,
                Quantidade = item.Quantidade
            }).ToList()
        };
}
