using OficinaMecanica.Execucao.Application.Inputs.Auth;
using OficinaMecanica.Execucao.Application.Outputs.Auth;
using OficinaMecanica.Execucao.InterfaceAdapters.DTOs;
using OficinaMecanica.Execucao.Application.UseCases.Auth;

namespace OficinaMecanica.Execucao.InterfaceAdapters.Controllers;

public class AuthCleanController
{
    private readonly LoginUseCase _loginUseCase;

    public AuthCleanController(LoginUseCase loginUseCase)
    {
        _loginUseCase = loginUseCase;
    }

    public LoginResponseDto? Login(LoginRequestDto loginRequestDto)
    {
        var output = _loginUseCase.Executar(new LoginInput(loginRequestDto.Username, loginRequestDto.Password));

        return output is null
            ? null
            : new LoginResponseDto
            {
                Token = output.Token,
                ExpiresAt = output.ExpiresAt
            };
    }
}
