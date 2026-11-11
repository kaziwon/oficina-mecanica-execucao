using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OficinaMecanica.Execucao.Controllers.Mappers;
using OficinaMecanica.Execucao.Domain.Entities;
using OficinaMecanica.Execucao.InterfaceAdapters.Controllers;
using OficinaMecanica.Execucao.InterfaceAdapters.DTOs;
using HttpDtos = OficinaMecanica.Execucao.Controllers.DTOs;

namespace OficinaMecanica.Execucao.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class ExecucoesController : ControllerBase
{
    private readonly ExecucoesCleanController _controller;
    public ExecucoesController(ExecucoesCleanController controller) { _controller = controller; }

    [HttpPost]
    [ProducesResponseType(typeof(ExecucaoResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public IActionResult Post([FromBody] HttpDtos.CriarExecucaoRequestDto dto)
    {
        var execucao = _controller.Criar(dto.ParaCleanDto());
        return CreatedAtAction(nameof(GetById), new { id = execucao.Id }, execucao);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ExecucaoResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetById(Guid id)
    {
        var execucao = _controller.ObterPorId(id);
        return execucao is null ? NotFound() : Ok(execucao);
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<ExecucaoResponseDto>), StatusCodes.Status200OK)]
    public IActionResult Get([FromQuery] StatusExecucao? status) => Ok(_controller.ObterFila(status));

    [HttpPost("{id:guid}/iniciar-diagnostico")]
    [ProducesResponseType(typeof(ExecucaoResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public IActionResult IniciarDiagnostico(Guid id)
    {
        var execucao = _controller.IniciarDiagnostico(id);
        return execucao is null ? NotFound() : Ok(execucao);
    }

    [HttpPost("{id:guid}/iniciar-reparo")]
    [ProducesResponseType(typeof(ExecucaoResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public IActionResult IniciarReparo(Guid id)
    {
        var execucao = _controller.IniciarReparo(id);
        return execucao is null ? NotFound() : Ok(execucao);
    }

    [HttpPost("{id:guid}/concluir-reparo")]
    [ProducesResponseType(typeof(ExecucaoResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public IActionResult ConcluirReparo(Guid id)
    {
        var execucao = _controller.ConcluirReparo(id);
        return execucao is null ? NotFound() : Ok(execucao);
    }

    [HttpPost("{id:guid}/concluir-diagnostico")]
    [ProducesResponseType(typeof(ExecucaoResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public IActionResult ConcluirDiagnostico(Guid id, [FromBody] HttpDtos.ConcluirDiagnosticoRequestDto dto)
    {
        var execucao = _controller.ConcluirDiagnostico(id, dto.ParaCleanDto());
        return execucao is null ? NotFound() : Ok(execucao);
    }
}
