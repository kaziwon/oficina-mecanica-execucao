using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OficinaMecanica.Execucao.Controllers.Mappers;
using OficinaMecanica.Execucao.InterfaceAdapters.Controllers;
using OficinaMecanica.Execucao.InterfaceAdapters.DTOs;
using HttpDtos = OficinaMecanica.Execucao.Controllers.DTOs;

namespace OficinaMecanica.Execucao.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class ServicosController : ControllerBase
{
    private readonly ServicosCleanController _servicosCleanController;

    public ServicosController(ServicosCleanController servicosCleanController)
    {
        _servicosCleanController = servicosCleanController;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<ServicoResponseDto>), StatusCodes.Status200OK)]
    public IActionResult Get()
    {
        return Ok(_servicosCleanController.ObterTodos());
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ServicoResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetById(Guid id)
    {
        var servico = _servicosCleanController.ObterPorId(id);

        return servico is null ? NotFound() : Ok(servico);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ServicoResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public IActionResult Post([FromBody] HttpDtos.ServicoRequestDto servicoRequestDto)
    {
        try
        {
            var servico = _servicosCleanController.Criar(servicoRequestDto.ParaCleanDto());
            return CreatedAtAction(nameof(GetById), new { id = servico.Id }, servico);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ServicoResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public IActionResult Put(Guid id, [FromBody] HttpDtos.ServicoRequestDto servicoRequestDto)
    {
        try
        {
            var servico = _servicosCleanController.Atualizar(id, servicoRequestDto.ParaCleanDto());

            return servico is null ? NotFound() : Ok(servico);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Delete(Guid id)
    {
        var removido = _servicosCleanController.Remover(id);

        return removido ? NoContent() : NotFound();
    }
}
