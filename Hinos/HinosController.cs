using Microsoft.AspNetCore.Mvc;

namespace AtasApi.Hinos;

[ApiController]
[Route("api/hinos")]
public class HinosController(IHinoService hinoService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? busca)
    {
        var hinos = await hinoService.GetAllAsync(busca);
        return Ok(hinos);
    }
}
