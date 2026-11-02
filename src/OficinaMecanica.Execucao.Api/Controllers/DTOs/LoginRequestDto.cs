using System.ComponentModel.DataAnnotations;

namespace OficinaMecanica.Execucao.Controllers.DTOs;

public class LoginRequestDto
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}
