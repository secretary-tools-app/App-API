using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AtasApi.Controllers;

// ─────────────────────────────────────────
// AppController  →  GET /api/app/info
// Informações do app (versão e contato).
// O contato de WhatsApp vem de config privada
// (appsettings.Development.json, ignorado pelo git)
// e só é servido a usuários autenticados.
// ─────────────────────────────────────────
[ApiController]
[Route("api/app")]
[Authorize]
public class AppController(IConfiguration config) : ControllerBase
{
    private const string Versao = "v1.0.0";

    [HttpGet("info")]
    public IActionResult GetInfo()
    {
        var contato = config["App:ContatoWhatsapp"];
        return Ok(new { versao = Versao, contatoWhatsapp = contato });
    }
}
