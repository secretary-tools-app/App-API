using System.Security.Cryptography;
using System.Text;
using AtasApi.DTOs;
using AtasApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AtasApi.Controllers;

// ─────────────────────────────────────────
// AdminController  →  PUT /api/admin/users/password
// Redefine senha de usuários via header X-Admin-Secret.
// Configure Admin__Secret (32+ caracteres) no ambiente.
// ─────────────────────────────────────────
[ApiController]
[Route("api/admin")]
public class AdminController(IAuthService authService, IConfiguration config) : ControllerBase
{
    private const string AdminSecretHeader = "X-Admin-Secret";

    /// <summary>Redefine a senha de um usuário (requer X-Admin-Secret).</summary>
    [HttpPut("users/password")]
    [EnableRateLimiting("admin")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ResetUserPassword([FromBody] AdminResetPasswordRequest req)
    {
        if (!IsAdminAuthorized())
            return StatusCode(403, new { message = "Acesso negado." });

        if (!ModelState.IsValid)
            return BadRequest(new { message = "Dados inválidos: informe usuário (3–50 chars) e senha (mín. 8 chars)." });

        var username = req.Username.Trim();
        var updated = await authService.AdminResetPasswordAsync(username, req.NewPassword);
        if (updated is null)
            return NotFound(new { message = "Usuário não encontrado." });

        if (updated == false)
            return StatusCode(500, new { message = "Não foi possível alterar a senha." });

        return Ok(new { message = "Senha alterada com sucesso.", username });
    }

    private bool IsAdminAuthorized()
    {
        var configured = config["Admin:Secret"];
        if (string.IsNullOrWhiteSpace(configured) || configured.Length < 32)
            return false;

        if (!Request.Headers.TryGetValue(AdminSecretHeader, out var providedValues))
            return false;

        var provided = providedValues.ToString();
        if (string.IsNullOrEmpty(provided))
            return false;

        var configuredBytes = Encoding.UTF8.GetBytes(configured);
        var providedBytes = Encoding.UTF8.GetBytes(provided);
        return configuredBytes.Length == providedBytes.Length
            && CryptographicOperations.FixedTimeEquals(configuredBytes, providedBytes);
    }
}
