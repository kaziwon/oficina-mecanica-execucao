namespace OficinaMecanica.Execucao.InterfaceAdapters.DTOs;

public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}
