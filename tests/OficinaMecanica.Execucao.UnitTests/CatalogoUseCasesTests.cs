using OficinaMecanica.Execucao.Application.Exceptions;
using OficinaMecanica.Execucao.Application.Gateways;
using OficinaMecanica.Execucao.Application.Inputs.Servicos;
using OficinaMecanica.Execucao.Application.Inputs.PecasInsumos;
using OficinaMecanica.Execucao.Application.UseCases.Servicos;
using OficinaMecanica.Execucao.Application.UseCases.PecasInsumos;
using OficinaMecanica.Execucao.UnitTests.Fakes;

namespace OficinaMecanica.Execucao.UnitTests;

public class CatalogoUseCasesTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Catalogos_RejeitamNomeVazio(string nome)
    {
        var fake = new CatalogoGatewayFake();
        Assert.Throws<ValidacaoException>(() => new CriarServicoUseCase(fake).Executar(new(nome, "", 10)));
        Assert.Throws<ValidacaoException>(() => new CriarPecaInsumoUseCase(fake).Executar(new(nome, "", 10, 1)));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("1.111")]
    [InlineData("1000000")]
    public void Catalogos_RejeitamPrecoInvalido(string valor)
    {
        var preco = decimal.Parse(valor, System.Globalization.CultureInfo.InvariantCulture);
        var fake = new CatalogoGatewayFake();
        Assert.Throws<ValidacaoException>(() => new CriarServicoUseCase(fake).Executar(new("Servico", "", preco)));
        Assert.Throws<ValidacaoException>(() => new CriarPecaInsumoUseCase(fake).Executar(new("Peca", "", preco, 1)));
    }

    [Fact]
    public void Catalogos_RejeitamDescricaoLongaENomeLongo()
    {
        var fake = new CatalogoGatewayFake();
        Assert.Throws<ValidacaoException>(() => new CriarServicoUseCase(fake).Executar(new(new string('a', 151), "", 10)));
        Assert.Throws<ValidacaoException>(() => new CriarPecaInsumoUseCase(fake).Executar(new("Peca", new string('a', 501), 10, 0)));
    }

    [Fact]
    public void Servico_CRUD_RejeitaDuplicatasEConsultaInexistentes()
    {
        var fake = new CatalogoGatewayFake();
        var criar = new CriarServicoUseCase(fake);
        var primeiro = criar.Executar(new("  Revisao  ", "Descricao", 15.55m));
        var segundo = criar.Executar(new("Freios", "", 20));
        Assert.Equal("Revisao", primeiro.Nome);
        Assert.Throws<ConflitoException>(() => criar.Executar(new("revisao", "", 10)));
        Assert.Throws<ConflitoException>(() => new AtualizarServicoUseCase(fake).Executar(segundo.Id, new("REVISAO", "", 20)));
        Assert.NotNull(new AtualizarServicoUseCase(fake).Executar(primeiro.Id, new("Revisao", "Nova", 25)));
        Assert.Null(new AtualizarServicoUseCase(fake).Executar(Guid.NewGuid(), new("Outro", "", 25)));
        Assert.Equal(2, new ObterTodosServicosUseCase(fake).Executar().Count);
        Assert.NotNull(new ObterServicoPorIdUseCase(fake).Executar(primeiro.Id));
        Assert.Null(new ObterServicoPorIdUseCase(fake).Executar(Guid.NewGuid()));
        Assert.True(new RemoverServicoUseCase(fake).Executar(primeiro.Id));
        Assert.False(new RemoverServicoUseCase(fake).Executar(primeiro.Id));
    }

    [Fact]
    public void Peca_AtualizarDados_NaoAlteraSaldoERejeitaDuplicatas()
    {
        var fake = new CatalogoGatewayFake();
        var criar = new CriarPecaInsumoUseCase(fake);
        var primeira = criar.Executar(new("  Filtro  ", "Descricao", 10.15m, 7));
        var segunda = criar.Executar(new("Oleo", "", 30, 5));
        Assert.Equal("Filtro", primeira.Nome);
        Assert.Throws<ConflitoException>(() => criar.Executar(new("filtro", "", 10, 0)));
        Assert.Throws<ConflitoException>(() => new AtualizarPecaInsumoUseCase(fake).Executar(segunda.Id, new("FILTRO", "", 15)));
        var atualizada = new AtualizarPecaInsumoUseCase(fake).Executar(primeira.Id, new("Filtro", "Nova", 20));
        Assert.Equal(7, atualizada!.QuantidadeEstoque);
        Assert.Null(new AtualizarPecaInsumoUseCase(fake).Executar(Guid.NewGuid(), new("Outra", "", 10)));
        Assert.Equal(2, new ObterTodasPecasInsumosUseCase(fake).Executar().Count);
        Assert.NotNull(new ObterPecaInsumoPorIdUseCase(fake).Executar(primeira.Id));
        Assert.Null(new ObterPecaInsumoPorIdUseCase(fake).Executar(Guid.NewGuid()));
        Assert.True(new RemoverPecaInsumoUseCase(fake).Executar(primeira.Id));
        Assert.False(new RemoverPecaInsumoUseCase(fake).Executar(primeira.Id));
    }

    [Fact]
    public void Peca_RejeitaEstoqueInicialNegativo()
    {
        Assert.Throws<ValidacaoException>(() => new CriarPecaInsumoUseCase(new CatalogoGatewayFake())
            .Executar(new("Filtro", "", 10, -1)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(int.MinValue)]
    public void MovimentarEstoque_RejeitaQuantidadeInvalida(int quantidade)
    {
        Assert.Throws<ValidacaoException>(() => new MovimentarEstoqueUseCase(new CatalogoGatewayFake()).Executar(Guid.NewGuid(), quantidade));
    }

    [Fact]
    public void MovimentarEstoque_TrataEntradaSaidaSaldoInsuficienteELimite()
    {
        var fake = new CatalogoGatewayFake();
        var peca = new CriarPecaInsumoUseCase(fake).Executar(new("Filtro", "", 10, 2));
        var useCase = new MovimentarEstoqueUseCase(fake);
        Assert.Equal(5, useCase.Executar(peca.Id, 3)!.QuantidadeEstoque);
        Assert.Equal(0, useCase.Executar(peca.Id, -5)!.QuantidadeEstoque);
        Assert.Throws<ConflitoException>(() => useCase.Executar(peca.Id, -1));
        Assert.Null(useCase.Executar(Guid.NewGuid(), 1));
        Assert.Equal(int.MaxValue, useCase.Executar(peca.Id, int.MaxValue)!.QuantidadeEstoque);
        Assert.Throws<ConflitoException>(() => useCase.Executar(peca.Id, 1));
    }
}
