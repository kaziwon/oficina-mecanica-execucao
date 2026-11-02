namespace OficinaMecanica.Execucao.Application.Exceptions;

public class ConflitoException : InvalidOperationException
{
    public ConflitoException(string message) : base(message) { }
}
