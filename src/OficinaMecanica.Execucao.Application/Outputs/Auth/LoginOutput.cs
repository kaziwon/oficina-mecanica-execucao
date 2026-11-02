using OficinaMecanica.Execucao.Application.Inputs.Auth;
namespace OficinaMecanica.Execucao.Application.Outputs.Auth;

public class LoginOutput
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}
