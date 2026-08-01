// ============================================================
// Controllers/AtasController.cs  (e todos os outros controllers)
// Mapeamento 1:1 com as rotas do app.py
// ============================================================
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using AtasApi.DTOs;
using AtasApi.Services;
using AtasApi.Repositories;
using AtasApi.Models;
using System.Globalization;
using System.Security.Claims;

namespace AtasApi.Controllers;

// ─────────────────────────────────────────
// AuthController  →  POST /api/auth/login
//                    POST /api/auth/logout
// ─────────────────────────────────────────
[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    /// <summary>Login de usuário. Retorna JWT.</summary>
    [HttpPost("login")]
    [EnableRateLimiting("login")]
    [ProducesResponseType(typeof(LoginResponse), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { message = "Usuário e senha são obrigatórios." });

        var result = await authService.LoginAsync(req.Username, req.Password);
        if (result is null)
            return Unauthorized(new { message = "Credenciais inválidas." });

        return Ok(result);
    }

    /// <summary>Registro de novo usuário com chave de convite.</summary>
    [HttpPost("register")]
    [EnableRateLimiting("register")]
    [ProducesResponseType(typeof(LoginResponse), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { message = "Dados inválidos: a senha deve ter no mínimo 8 caracteres e o usuário entre 3 e 50." });

        var result = await authService.RegisterAsync(req.Username, req.Password, req.InviteKey);
        if (result is null)
            return BadRequest(new { message = "Usuário já existe ou chave de convite inválida." });

        return Ok(result);
    }

    /// <summary>Troca a senha do usuário logado (valida a senha atual).</summary>
    [HttpPut("password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { message = "Dados inválidos: a nova senha deve ter no mínimo 8 caracteres." });

        var userIdClaim = User?.FindFirst("sub");
        if (userIdClaim is null || !int.TryParse(userIdClaim.Value, out var userId) || userId <= 0)
            return Unauthorized();

        var ok = await authService.ChangePasswordAsync(userId, req.CurrentPassword, req.NewPassword);
        if (!ok)
            return BadRequest(new { message = "Senha atual incorreta ou nova senha inválida." });

        return Ok(new { message = "Senha alterada com sucesso." });
    }

    /// <summary>Logout (client-side: apenas invalida token localmente).</summary>
    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout() => Ok(new { message = "Logout realizado." });
}

// ─────────────────────────────────────────
// AtasController
//   GET    /api/atas?mes=YYYY-MM      → lista do mês
//   GET    /api/atas/all              → todas da ala
//   GET    /api/atas/{id}             → uma ata
//   POST   /api/atas                  → criar
//   PUT    /api/atas/{id}             → atualizar
//   DELETE /api/atas/{id}             → excluir (com cascade)
//   GET    /api/atas/by-data?data=&tipo=  → busca por data+tipo
// ─────────────────────────────────────────
[ApiController]
[Route("api/atas")]
[Authorize]
public class AtasController(
    IAtaService ataService,
    ISacramentalService sacService,
    IBatismoService batService,
    ISacramentalRepository sacRepo,
    IBatismoRepository batRepo,
    IAtaRepository ataRepo) : ControllerBase
{
    private int AlaId
    {
        get
        {
            var claim = User?.FindFirst("ala_id");
            return claim != null && int.TryParse(claim.Value, out var id) ? id : 0;
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetByMes([FromQuery] string mes)
    {
        var atas = await ataService.GetByMesAsync(AlaId, mes);
        return Ok(atas);
    }

    [HttpGet("all")]
    public async Task<IActionResult> GetAll()
    {
        var atas = await ataService.GetAllAsync(AlaId);
        return Ok(atas);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var ata = await ataService.GetByIdAsync(id, AlaId);
        return ata is null ? NotFound() : Ok(ata);
    }

    [HttpGet("by-data")]
    public async Task<IActionResult> GetByDataTipo([FromQuery] string data, [FromQuery] string tipo)
    {
        var ata = await ataService.GetByDataTipoAsync(data, tipo, AlaId);
        return ata is null ? NotFound() : Ok(ata);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAtaRequest req)
    {
        // Verifica se já existe ata sacramental para a data
        if (req.Tipo == "sacramental")
        {
            var existing = await ataService.GetByDataTipoAsync(req.Data, "sacramental", AlaId);
            if (existing is not null)
                return CreatedAtAction(nameof(GetById), new { id = existing.Id }, existing); // Modificado aqui
        }

        var ata = await ataService.CreateAsync(AlaId, req);
        return CreatedAtAction(nameof(GetById), new { id = ata.Id }, ata);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateAtaRequest req)
    {
        var ata = await ataService.UpdateAsync(id, AlaId, req);
        return ata is null ? NotFound() : Ok(ata);
    }

    /// <summary>
    /// Exclui a ata e seus detalhes (sacramental ou batismo) em transação.
    /// Equivale ao deletar_ata() do Python.
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var ata = await ataService.GetByIdAsync(id, AlaId);
        if (ata is null) return NotFound();

        if (ata.Tipo == "sacramental")
            await sacRepo.DeleteByAtaIdAsync(id);
        else
            await batRepo.DeleteByAtaIdAsync(id);

        await ataService.DeleteAsync(id, AlaId);
        return NoContent();
    }
}

// ─────────────────────────────────────────
// SacramentalController
//   GET    /api/sacramental/{ataId}
//   POST   /api/sacramental
//   PUT    /api/sacramental/{ataId}
//   DELETE /api/sacramental/{ataId}
// ─────────────────────────────────────────
[ApiController]
[Route("api/sacramental")]
[Authorize]
public class SacramentalController(ISacramentalService sacService, IAtaRepository ataRepo) : ControllerBase
{
    private int AlaId
    {
        get
        {
            var claim = User?.FindFirst("ala_id");
            return claim != null && int.TryParse(claim.Value, out var id) ? id : 0;
        }
    }

    private async Task<bool> AtaPertenceALaAsync(int ataId) =>
        await ataRepo.GetByIdAsync(ataId, AlaId) is not null;

    /// <summary>
    /// Status de uma ata sacramental: primeiro domingo do mês (jejum/testemunhos,
    /// dia &lt;= 7) conclui com ao menos um testemunho; os demais, com os dois
    /// discursantes preenchidos.
    /// </summary>
    private static string StatusDaAta(Ata ata, CreateSacramentalRequest req)
    {
        if (DateTime.TryParseExact(ata.Data, "yyyy-MM-dd",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var data) &&
            data.Day <= 7 && data.DayOfWeek == DayOfWeek.Sunday)
        {
            return SacramentalService.StatusParaPrimeiroDomingo(req.Testemunhos);
        }
        return SacramentalService.StatusParaDiscursantes(req.Discursante1, req.Discursante2);
    }

    [HttpGet("{ataId:int}")]
    public async Task<IActionResult> Get(int ataId)
    {
        if (!await AtaPertenceALaAsync(ataId)) return NotFound();
        var s = await sacService.GetByAtaIdAsync(ataId);
        return s is null ? NotFound() : Ok(s);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSacramentalRequest req)
    {
        var ata = await ataRepo.GetByIdAsync(req.AtaId, AlaId);
        if (ata is null) return NotFound();
        var s = await sacService.CreateAsync(req);
        await ataRepo.UpdateStatusAsync(req.AtaId, StatusDaAta(ata, req));
        return CreatedAtAction(nameof(Get), new { ataId = s.AtaId }, s);
    }

    [HttpPut("{ataId:int}")]
    public async Task<IActionResult> Update(int ataId, [FromBody] CreateSacramentalRequest req)
    {
        var ata = await ataRepo.GetByIdAsync(ataId, AlaId);
        if (ata is null) return NotFound();
        var s = await sacService.UpdateAsync(ataId, req);
        if (s is null) return NotFound();
        await ataRepo.UpdateStatusAsync(ataId, StatusDaAta(ata, req));
        return Ok(s);
    }

    [HttpDelete("{ataId:int}")]
    public async Task<IActionResult> Delete(int ataId)
    {
        if (!await AtaPertenceALaAsync(ataId)) return NotFound();
        await sacService.DeleteByAtaIdAsync(ataId);
        return NoContent();
    }
}

// ─────────────────────────────────────────
// BatismoController
//   GET    /api/batismo/{ataId}
//   POST   /api/batismo
//   PUT    /api/batismo/{ataId}
//   DELETE /api/batismo/{ataId}
// ─────────────────────────────────────────
[ApiController]
[Route("api/batismo")]
[Authorize]
public class BatismoController(IBatismoService batService, IAtaRepository ataRepo) : ControllerBase
{
    private int AlaId
    {
        get
        {
            var claim = User?.FindFirst("ala_id");
            return claim != null && int.TryParse(claim.Value, out var id) ? id : 0;
        }
    }

    private async Task<bool> AtaPertenceALaAsync(int ataId) =>
        await ataRepo.GetByIdAsync(ataId, AlaId) is not null;

    [HttpGet("{ataId:int}")]
    public async Task<IActionResult> Get(int ataId)
    {
        if (!await AtaPertenceALaAsync(ataId)) return NotFound();
        var b = await batService.GetByAtaIdAsync(ataId);
        return b is null ? NotFound() : Ok(b);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBatismoRequest req)
    {
        if (!await AtaPertenceALaAsync(req.AtaId)) return NotFound();
        var b = await batService.CreateAsync(req);
        return CreatedAtAction(nameof(Get), new { ataId = b.AtaId }, b);
    }

    [HttpPut("{ataId:int}")]
    public async Task<IActionResult> Update(int ataId, [FromBody] CreateBatismoRequest req)
    {
        if (!await AtaPertenceALaAsync(ataId)) return NotFound();
        var b = await batService.UpdateAsync(ataId, req);
        return b is null ? NotFound() : Ok(b);
    }

    [HttpDelete("{ataId:int}")]
    public async Task<IActionResult> Delete(int ataId)
    {
        if (!await AtaPertenceALaAsync(ataId)) return NotFound();
        await batService.DeleteByAtaIdAsync(ataId);
        return NoContent();
    }
}

// ─────────────────────────────────────────
// DiscursantesController
//   POST /api/discursantes/salvar      → save_discursantes_temas()
//   GET  /api/discursantes/state       → api_discursantes_state()
//   GET  /api/discursantes/recentes    → get_discursantes_recentes()
// ─────────────────────────────────────────
[ApiController]
[Route("api/discursantes")]
[Authorize]
public class DiscursantesController(
    ISacramentalService sacService,
    IAtaRepository ataRepo) : ControllerBase
{
    private int AlaId
    {
        get
        {
            var claim = User?.FindFirst("ala_id");
            return claim != null && int.TryParse(claim.Value, out var id) ? id : 0;
        }
    }

    [HttpPost("salvar")]
    public async Task<IActionResult> Salvar([FromBody] SaveDiscursantesRequest req)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { message = "Dados inválidos." });
        }

        if (!DateTime.TryParse(req.Date, out var dt))
        {
            return BadRequest(new { message = "Data inválida." });
        }

        if (dt.DayOfWeek != DayOfWeek.Sunday)
        {
            return BadRequest(new { message = "A data deve ser um domingo." });
        }

        if (dt.Day <= 7 && (req.Discursante1 != null || req.Discursante2 != null || req.Discursante3 != null))
        {
            return BadRequest(new { message = "Primeiro domingo é reunião de testemunhos — não há discursos." });
        }

        var result = await sacService.SaveDiscursantesAsync(AlaId, req, ataRepo);
        return Ok(result);
    }

    [HttpGet("state")]
    public async Task<IActionResult> GetState([FromQuery] string? date, [FromQuery] int? ataId)
    {
        if (ataId.HasValue)
        {
            var s = await sacService.GetByAtaIdAsync(ataId.Value);
            if (s is null) return Ok(new { });
            return Ok(new DiscursantesStateResponse
            {
                AtaId = s.AtaId, Discursante1 = s.Discursante1, Discursante2 = s.Discursante2,
                Discursante3 = s.UltimoDiscursante, Tema = s.Tema,
                Tema1 = s.Tema1, Tema2 = s.Tema2, Tema3 = s.TemaUltimo,
                Obs1 = s.Obs1, Obs2 = s.Obs2, Obs3 = s.ObsUltimo,
                HinoAbertura = s.HinoAbertura, HinoSacramental = s.HinoSacramental,
                HinoIntermediario = s.HinoIntermediario, HinoEncerramento = s.HinoEncerramento,
                Date = date
            });
        }

        if (!string.IsNullOrWhiteSpace(date))
        {
            var ata = await ataRepo.GetByDataTipoAsync(date, "sacramental", AlaId);
            if (ata is null) return Ok(new { });
            var s = await sacService.GetByAtaIdAsync(ata.Id);
            if (s is null) return Ok(new { });
            return Ok(new DiscursantesStateResponse
            {
                AtaId = s.AtaId, Date = date,
                Discursante1 = s.Discursante1, Discursante2 = s.Discursante2,
                Discursante3 = s.UltimoDiscursante, Tema = s.Tema,
                Tema1 = s.Tema1, Tema2 = s.Tema2, Tema3 = s.TemaUltimo,
                Obs1 = s.Obs1, Obs2 = s.Obs2, Obs3 = s.ObsUltimo,
                HinoAbertura = s.HinoAbertura, HinoSacramental = s.HinoSacramental,
                HinoIntermediario = s.HinoIntermediario, HinoEncerramento = s.HinoEncerramento
            });
        }

        return BadRequest(new { message = "Informe 'date' ou 'ataId'." });
    }

    [HttpGet("recentes")]
    public async Task<IActionResult> GetRecentes([FromQuery] int dias = 90)
    {
        var result = await sacService.GetDiscursantesRecentesAsync(AlaId, dias);
        return Ok(result);
    }

    [HttpGet("sugestoes")]
    public async Task<IActionResult> GetSugestoes()
    {
        var result = await sacService.GetSugestoesAsync(AlaId);
        return Ok(result);
    }
}

// ─────────────────────────────────────────
// ConfiguracoesController
//   GET  /api/configuracoes/templates           → listar templates da ala
//   GET  /api/configuracoes/templates/{id}      → um template
//   POST /api/configuracoes/templates           → criar
//   PUT  /api/configuracoes/templates/{id}      → salvar
//   DELETE /api/configuracoes/templates/{id}    → apagar
//   GET  /api/configuracoes/unidade             → dados da ala
//   PUT  /api/configuracoes/unidade             → salvar config da ala
//   GET  /api/configuracoes/estatisticas        → contagens
// ─────────────────────────────────────────
[ApiController]
[Route("api/configuracoes")]
[Authorize]
public class ConfiguracoesController(
    ITemplateService templateService,
    IUnidadeService unidadeService,
    IAtaService ataService) : ControllerBase
{
    private int AlaId
    {
        get
        {
            var claim = User?.FindFirst("ala_id");
            return claim != null && int.TryParse(claim.Value, out var id) ? id : 0;
        }
    }

    [HttpGet("templates")]
    public async Task<IActionResult> GetTemplates()
    {
        var templates = await templateService.GetTemplatesAsync(AlaId);
        return Ok(templates);
    }

    [HttpGet("templates/{id:int}")]
    public async Task<IActionResult> GetTemplate(int id)
    {
        var t = await templateService.GetByIdAsync(id);
        if (t is null || t.AlaId != AlaId) return NotFound();
        return Ok(t);
    }

    [HttpPost("templates")]
    public async Task<IActionResult> CreateTemplate([FromBody] SaveTemplateRequest req)
    {
        var t = await templateService.CreateAsync(AlaId, req);
        return CreatedAtAction(nameof(GetTemplate), new { id = t.Id }, t);
    }

    [HttpPut("templates/{id:int}")]
    public async Task<IActionResult> UpdateTemplate(int id, [FromBody] SaveTemplateRequest req)
    {
        var t = await templateService.UpdateAsync(id, AlaId, req);
        return t is null ? NotFound() : Ok(t);
    }

    [HttpDelete("templates/{id:int}")]
    public async Task<IActionResult> DeleteTemplate(int id)
    {
        var ok = await templateService.DeleteAsync(id, AlaId);
        return ok ? NoContent() : NotFound();
    }

    [HttpGet("unidade")]
    public async Task<IActionResult> GetUnidade()
    {
        var u = await unidadeService.GetAsync(AlaId);
        return u is null ? NotFound() : Ok(u);
    }

    [HttpPut("unidade")]
    public async Task<IActionResult> SaveUnidade([FromBody] SaveUnidadeRequest req)
    {
        var u = await unidadeService.UpsertAsync(AlaId, req);
        return Ok(u);
    }

    [HttpGet("estatisticas")]
    public async Task<IActionResult> GetEstatisticas()
    {
        var all = (await ataService.GetAllAsync(AlaId)).ToList();
        var mesAtual = DateTime.Now.ToString("yyyy-MM");
        return Ok(new
        {
            TotalAtas = all.Count,
            AtasSacramentais = all.Count(a => a.Tipo == "sacramental"),
            AtasBatismo = all.Count(a => a.Tipo == "batismo"),
            AtasMesAtual = all.Count(a => a.Data.StartsWith(mesAtual))
        });
    }
}

// ─────────────────────────────────────────
// TarefasController
//   GET    /api/tarefas              → listar tarefas da ala
//   GET    /api/tarefas/{id}         → uma tarefa
//   POST   /api/tarefas              → criar
//   PUT    /api/tarefas/{id}         → atualizar
//   DELETE /api/tarefas/{id}         → excluir
//   PATCH  /api/tarefas/{id}/toggle  → alternar concluida
// ─────────────────────────────────────────
[ApiController]
[Route("api/tarefas")]
[Authorize]
public class TarefasController(ITarefaService tarefaService) : ControllerBase
{
    private int AlaId
    {
        get
        {
            var claim = User?.FindFirst("ala_id");
            return claim != null && int.TryParse(claim.Value, out var id) ? id : 0;
        }
    }

    private string Role => User?.FindFirst("role")?.Value ?? "";

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var tarefas = await tarefaService.GetAllAsync(AlaId, Role);
        return Ok(tarefas);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var tarefa = await tarefaService.GetByIdAsync(id, AlaId, Role);
        return tarefa is null ? NotFound() : Ok(tarefa);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTarefaRequest req)
    {
        var tarefa = await tarefaService.CreateAsync(AlaId, Role, req);
        return CreatedAtAction(nameof(GetById), new { id = tarefa.Id }, tarefa);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTarefaRequest req)
    {
        var tarefa = await tarefaService.UpdateAsync(id, AlaId, Role, req);
        return tarefa is null ? NotFound() : Ok(tarefa);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var ok = await tarefaService.DeleteAsync(id, AlaId, Role);
        return ok ? NoContent() : NotFound();
    }

    [HttpPatch("{id:int}/toggle")]
    public async Task<IActionResult> Toggle(int id)
    {
        var tarefa = await tarefaService.GetByIdAsync(id, AlaId, Role);
        if (tarefa is null) return NotFound();

        var updated = await tarefaService.UpdateAsync(id, AlaId, Role, new UpdateTarefaRequest
        {
            Concluida = !tarefa.Concluida
        });
        return Ok(updated);
    }
}

// ─────────────────────────────────────────
// UsuariosController
//   GET  /api/usuarios        → usuários da ala (tags de tarefas)
//   PUT  /api/usuarios/me     → atualiza primeiro nome do usuário logado
// ─────────────────────────────────────────
[ApiController]
[Route("api/usuarios")]
[Authorize]
public class UsuariosController(IUsuarioService usuarioService) : ControllerBase
{
    private int AlaId
    {
        get
        {
            var claim = User?.FindFirst("ala_id");
            return claim != null && int.TryParse(claim.Value, out var id) ? id : 0;
        }
    }

    private int UserId
    {
        get
        {
            var claim = User?.FindFirst("sub");
            return claim != null && int.TryParse(claim.Value, out var id) ? id : 0;
        }
    }

    private string Role => User?.FindFirst("role")?.Value ?? "";

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var usuarios = await usuarioService.GetByAlaAsync(AlaId, Role);
        return Ok(usuarios);
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateProfileRequest req)
    {
        if (UserId <= 0) return Unauthorized();

        var usuario = await usuarioService.UpdateDisplayNameAsync(UserId, req.DisplayName);
        return usuario is null ? NotFound() : Ok(usuario);
    }
}
