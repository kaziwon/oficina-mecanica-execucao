using OficinaMecanica.Execucao.Application.Exceptions;

namespace OficinaMecanica.Execucao.Application.Validators;

internal static class CatalogoValidator
{
    public static void Validar(string nome, string descricao, decimal preco)
    {
        if (string.IsNullOrWhiteSpace(nome) || nome.Trim().Length > 150)
            throw new ValidacaoException("Informe um nome com ate 150 caracteres.");
        if (descricao is null || descricao.Length > 500)
            throw new ValidacaoException("A descricao deve ter ate 500 caracteres.");
        if (preco <= 0 || preco > 999999.99m || decimal.Round(preco, 2) != preco)
            throw new ValidacaoException("Informe um preco positivo com ate duas casas decimais e no maximo 999999.99.");
    }
}
