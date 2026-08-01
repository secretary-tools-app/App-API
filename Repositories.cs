// ============================================================
// Repositories/AtaRepository.cs
// Todas as queries SQL mapeadas do app.py
// ============================================================
using System.Data;
using Dapper;
using AtasApi.Data;
using AtasApi.Models;
using AtasApi.DTOs;

namespace AtasApi.Repositories;

// ─────────────────────────────────────────
// Interfaces
// ─────────────────────────────────────────
public interface IAtaRepository
{
    Task<IEnumerable<Ata>> GetByMesAsync(int alaId, string anoMes);       // "YYYY-MM"
    Task<IEnumerable<Ata>> GetAllByAlaAsync(int alaId);
    Task<Ata?> GetByIdAsync(int id, int alaId);
    Task<Ata?> GetByDataTipoAsync(string data, string tipo, int alaId);
    Task<int> CreateAsync(Ata ata);
    Task UpdateAsync(Ata ata);
    Task UpdateStatusAsync(int id, string status);
    Task DeleteAsync(int id, int alaId);
}

public interface ISacramentalRepository
{
    Task<Sacramental?> GetByAtaIdAsync(int ataId);
    Task<int> CreateAsync(Sacramental s);
    Task UpdateAsync(Sacramental s);
    Task DeleteByAtaIdAsync(int ataId);
    Task<IEnumerable<Sacramental>> GetRecentesAsync(int alaId, string dataLimite);
    Task<IEnumerable<DiscursanteSugestao>> GetSugestoesAsync(int alaId);
}

public interface IBatismoRepository
{
    Task<Batismo?> GetByAtaIdAsync(int ataId);
    Task<int> CreateAsync(Batismo b);
    Task UpdateAsync(Batismo b);
    Task DeleteByAtaIdAsync(int ataId);
}

public interface ITemplateRepository
{
    Task<IEnumerable<Template>> GetByAlaAsync(int alaId);
    Task<Template?> GetByIdAsync(int id);
    Task<Template?> GetPadraoAsync(int tipoTemplate);           // ala_id = 0
    Task<int> CreateAsync(Template t);
    Task UpdateAsync(Template t);
    Task DeleteAsync(int id);
    Task CloneDefaultsForAlaAsync(int alaId);
}

public interface IUnidadeRepository
{
    Task<Unidade?> GetByAlaAsync(int alaId);
    Task UpsertAsync(Unidade u);
}

public interface IUserRepository
{
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByIdAsync(int id);
    Task<int> CreateAsync(User user);
    Task<IEnumerable<User>> GetByAlaAsync(int alaId, string role);
    Task<bool> UpdateDisplayNameAsync(int id, string? displayName);
    Task<bool> UpdatePasswordAsync(int id, string newPasswordHash);
}

public interface IAlaKeyRepository
{
    Task<AlaKey?> GetByKeyAsync(string key);
}

public interface ITarefaRepository
{
    Task<IEnumerable<Tarefa>> GetByAlaAsync(int alaId, string role);
    Task<Tarefa?> GetByIdAsync(int id, int alaId);
    Task<int> CreateAsync(Tarefa t);
    Task UpdateAsync(Tarefa t);
    Task DeleteAsync(int id, int alaId);
}

// ─────────────────────────────────────────
// Implementações
// ─────────────────────────────────────────

public class AtaRepository(IDbContext db) : IAtaRepository
{
    public async Task<IEnumerable<Ata>> GetByMesAsync(int alaId, string anoMes)
    {
        using var conn = db.CreateConnection();
        return await conn.QueryAsync<Ata>(
            @"SELECT * FROM atas
              WHERE strftime('%Y-%m', data) = @AnoMes
                AND ala_id = @AlaId
              ORDER BY data DESC",
            new { AnoMes = anoMes, AlaId = alaId });
    }

    public async Task<IEnumerable<Ata>> GetAllByAlaAsync(int alaId)
    {
        using var conn = db.CreateConnection();
        return await conn.QueryAsync<Ata>(
            "SELECT * FROM atas WHERE ala_id = @AlaId ORDER BY data DESC",
            new { AlaId = alaId });
    }

    public async Task<Ata?> GetByIdAsync(int id, int alaId)
    {
        using var conn = db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<Ata>(
            "SELECT * FROM atas WHERE id = @Id AND ala_id = @AlaId",
            new { Id = id, AlaId = alaId });
    }

    public async Task<Ata?> GetByDataTipoAsync(string data, string tipo, int alaId)
    {
        using var conn = db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<Ata>(
            "SELECT * FROM atas WHERE data = @Data AND tipo = @Tipo AND ala_id = @AlaId",
            new { Data = data, Tipo = tipo, AlaId = alaId });
    }

    public async Task<int> CreateAsync(Ata ata)
    {
        using var conn = db.CreateConnection();
        return await conn.QuerySingleAsync<int>(
            @"INSERT INTO atas (tipo, data, status, ala_id)
              VALUES (@Tipo, @Data, @Status, @AlaId)
              RETURNING id",
            ata);
    }

    public async Task UpdateAsync(Ata ata)
    {
        using var conn = db.CreateConnection();
        await conn.ExecuteAsync(
            "UPDATE atas SET tipo = @Tipo, data = @Data, status = @Status WHERE id = @Id",
            ata);
    }

    public async Task UpdateStatusAsync(int id, string status)
    {
        using var conn = db.CreateConnection();
        await conn.ExecuteAsync(
            "UPDATE atas SET status = @Status WHERE id = @Id",
            new { Id = id, Status = status });
    }

    public async Task DeleteAsync(int id, int alaId)
    {
        using var conn = db.CreateConnection();
        await conn.ExecuteAsync(
            "DELETE FROM atas WHERE id = @Id AND ala_id = @AlaId",
            new { Id = id, AlaId = alaId });
    }
}

public class SacramentalRepository(IDbContext db) : ISacramentalRepository
{
    public async Task<Sacramental?> GetByAtaIdAsync(int ataId)
    {
        using var conn = db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<Sacramental>(
            "SELECT * FROM sacramental WHERE ata_id = @AtaId",
            new { AtaId = ataId });
    }

    public async Task<int> CreateAsync(Sacramental s)
    {
        using var conn = db.CreateConnection();
        return await conn.QuerySingleAsync<int>(@"
            INSERT INTO sacramental (
                ata_id, presidido, dirigido, pianista, regente_musica,
                anuncios, hinos, hino_sacramental, hino_intermediario, oracoes,
                discursante_1, discursante_2, outros, tema_1, tema_2, tema_ultimo,
                obs_1, obs_2, obs_ultimo, recepcionistas, reconhecemos_presenca,
                desobrigacoes, apoios, confirmacoes_batismo, apoio_membros,
                bencao_criancas, testemunhos, ultimo_discursante, tema
            ) VALUES (
                @AtaId, @Presidido, @Dirigido, @Pianista, @RegentMusica,
                @Anuncios, @Hinos, @HinoSacramental, @HinoIntermediario, @Oracoes,
                @Discursante1, @Discursante2, @Outros, @Tema1, @Tema2, @TemaUltimo,
                @Obs1, @Obs2, @ObsUltimo, @Recepcionistas, @ReconhecemosPresenca,
                @Desobrigacoes, @Apoios, @ConfirmacoesBatismo, @ApoioMembros,
                @BencaoCriancas, @Testemunhos, @UltimoDiscursante, @Tema
            ) RETURNING id", s);
    }

    public async Task UpdateAsync(Sacramental s)
    {
        using var conn = db.CreateConnection();
        await conn.ExecuteAsync(@"
            UPDATE sacramental SET
                presidido=@Presidido, dirigido=@Dirigido, pianista=@Pianista,
                regente_musica=@RegentMusica, anuncios=@Anuncios, hinos=@Hinos,
                hino_sacramental=@HinoSacramental, hino_intermediario=@HinoIntermediario,
                oracoes=@Oracoes, discursante_1=@Discursante1, discursante_2=@Discursante2,
                outros=@Outros, tema_1=@Tema1, tema_2=@Tema2, tema_ultimo=@TemaUltimo,
                obs_1=@Obs1, obs_2=@Obs2, obs_ultimo=@ObsUltimo,
                recepcionistas=@Recepcionistas, reconhecemos_presenca=@ReconhecemosPresenca,
                desobrigacoes=@Desobrigacoes, apoios=@Apoios,
                confirmacoes_batismo=@ConfirmacoesBatismo, apoio_membros=@ApoioMembros,
                bencao_criancas=@BencaoCriancas, testemunhos=@Testemunhos,
                ultimo_discursante=@UltimoDiscursante, tema=@Tema
            WHERE ata_id=@AtaId", s);
    }

    public async Task DeleteByAtaIdAsync(int ataId)
    {
        using var conn = db.CreateConnection();
        await conn.ExecuteAsync(
            "DELETE FROM sacramental WHERE ata_id = @AtaId",
            new { AtaId = ataId });
    }

    public async Task<IEnumerable<Sacramental>> GetRecentesAsync(int alaId, string dataLimite)
    {
        using var conn = db.CreateConnection();
        return await conn.QueryAsync<Sacramental>(@"
            SELECT s.*, a.data AS date FROM sacramental s
            JOIN atas a ON s.ata_id = a.id
            WHERE a.data >= @DataLimite
              AND a.tipo = 'sacramental'
              AND a.ala_id = @AlaId
            ORDER BY a.data DESC",
            new { DataLimite = dataLimite, AlaId = alaId });
    }

    public async Task<IEnumerable<DiscursanteSugestao>> GetSugestoesAsync(int alaId)
    {
        using var conn = db.CreateConnection();
        return await conn.QueryAsync<DiscursanteSugestao>(@"
            SELECT nome, ultima_data AS ultimaData, posicao FROM (
              SELECT discursante_1 AS nome, a.data AS ultima_data, '1º' AS posicao
              FROM sacramental s
              JOIN atas a ON s.ata_id = a.id
              WHERE s.discursante_1 IS NOT NULL AND s.discursante_1 != ''
                AND a.ala_id = @AlaId AND a.tipo = 'sacramental'
              UNION ALL
              SELECT discursante_2, a.data, '2º'
              FROM sacramental s
              JOIN atas a ON s.ata_id = a.id
              WHERE s.discursante_2 IS NOT NULL AND s.discursante_2 != ''
                AND a.ala_id = @AlaId AND a.tipo = 'sacramental'
              UNION ALL
              SELECT ultimo_discursante, a.data, '3º'
              FROM sacramental s
              JOIN atas a ON s.ata_id = a.id
              WHERE s.ultimo_discursante IS NOT NULL AND s.ultimo_discursante != ''
                AND a.ala_id = @AlaId AND a.tipo = 'sacramental'
            )
            GROUP BY nome
            ORDER BY MAX(ultima_data) DESC",
            new { AlaId = alaId });
    }
}

public class BatismoRepository(IDbContext db) : IBatismoRepository
{
    public async Task<Batismo?> GetByAtaIdAsync(int ataId)
    {
        using var conn = db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<Batismo>(
            "SELECT * FROM batismo WHERE ata_id = @AtaId",
            new { AtaId = ataId });
    }

    public async Task<int> CreateAsync(Batismo b)
    {
        using var conn = db.CreateConnection();
        return await conn.QuerySingleAsync<int>(@"
            INSERT INTO batismo (ata_id, dedicado, presidido, dirigido, batizados,
                                 testemunha1, testemunha2, programa)
            VALUES (@AtaId, @Dedicado, @Presidido, @Dirigido, @Batizados,
                    @Testemunha1, @Testemunha2, @Programa)
            RETURNING id", b);
    }

    public async Task UpdateAsync(Batismo b)
    {
        using var conn = db.CreateConnection();
        await conn.ExecuteAsync(@"
            UPDATE batismo SET
                dedicado=@Dedicado, presidido=@Presidido, dirigido=@Dirigido,
                batizados=@Batizados, testemunha1=@Testemunha1, testemunha2=@Testemunha2,
                programa=@Programa
            WHERE ata_id=@AtaId", b);
    }

    public async Task DeleteByAtaIdAsync(int ataId)
    {
        using var conn = db.CreateConnection();
        await conn.ExecuteAsync(
            "DELETE FROM batismo WHERE ata_id = @AtaId",
            new { AtaId = ataId });
    }
}

public class TemplateRepository(IDbContext db) : ITemplateRepository
{
    public async Task<IEnumerable<Template>> GetByAlaAsync(int alaId)
    {
        using var conn = db.CreateConnection();
        return await conn.QueryAsync<Template>(
            "SELECT * FROM templates WHERE ala_id = @AlaId",
            new { AlaId = alaId });
    }

    public async Task<Template?> GetByIdAsync(int id)
    {
        using var conn = db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<Template>(
            "SELECT * FROM templates WHERE id = @Id",
            new { Id = id });
    }

    public async Task<Template?> GetPadraoAsync(int tipoTemplate)
    {
        using var conn = db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<Template>(
            "SELECT * FROM templates WHERE ala_id = 0 AND tipo_template = @Tipo LIMIT 1",
            new { Tipo = tipoTemplate });
    }

    public async Task<int> CreateAsync(Template t)
    {
        using var conn = db.CreateConnection();
        return await conn.QuerySingleAsync<int>(@"
            INSERT INTO templates (ala_id, tipo_template, nome, boas_vindas, desobrigacoes,
                apoios, confirmacoes_batismo, apoio_membro_novo, bencao_crianca, ordenacoes, sacramento,
                mensagens, live, encerramento,
                desobrigacoes_plural, apoios_plural, confirmacoes_batismo_plural, apoio_membro_novo_plural, bencao_crianca_plural, ordenacoes_plural)
            VALUES (@AlaId, @TipoTemplate, @Nome, @BoasVindas, @Desobrigacoes,
                @Apoios, @ConfirmacoesBatismo, @ApoioMembroNovo, @BencaoCrianca, @Ordenacoes, @Sacramento,
                @Mensagens, @Live, @Encerramento,
                @DesobrigacoesPlural, @ApoiosPlural, @ConfirmacoesBatismoPlural, @ApoioMembroNovoPlural, @BencaoCriancaPlural, @OrdenacoesPlural)
            RETURNING id", t);
    }

    public async Task UpdateAsync(Template t)
    {
        using var conn = db.CreateConnection();
        await conn.ExecuteAsync(@"
            UPDATE templates SET
                nome=@Nome, boas_vindas=@BoasVindas, desobrigacoes=@Desobrigacoes,
                apoios=@Apoios, confirmacoes_batismo=@ConfirmacoesBatismo,
                apoio_membro_novo=@ApoioMembroNovo, bencao_crianca=@BencaoCrianca,
                ordenacoes=@Ordenacoes, sacramento=@Sacramento, mensagens=@Mensagens, live=@Live, encerramento=@Encerramento,
                desobrigacoes_plural=@DesobrigacoesPlural, apoios_plural=@ApoiosPlural,
                confirmacoes_batismo_plural=@ConfirmacoesBatismoPlural, apoio_membro_novo_plural=@ApoioMembroNovoPlural,
                bencao_crianca_plural=@BencaoCriancaPlural, ordenacoes_plural=@OrdenacoesPlural
            WHERE id=@Id AND ala_id=@AlaId", t);
    }

    public async Task DeleteAsync(int id)
    {
        using var conn = db.CreateConnection();
        await conn.ExecuteAsync("DELETE FROM templates WHERE id = @Id", new { Id = id });
    }

    /// <summary>
    /// Copia os templates padrão (ala_id=0) para a ala especificada.
    /// Equivale ao bloco de clonagem em configuracoes() do Python.
    /// </summary>
    public async Task CloneDefaultsForAlaAsync(int alaId)
    {
        using var conn = db.CreateConnection();
        await conn.ExecuteAsync(@"
            INSERT INTO templates (ala_id, tipo_template, nome, boas_vindas, desobrigacoes,
                apoios, confirmacoes_batismo, apoio_membro_novo, bencao_crianca, ordenacoes, sacramento,
                mensagens, live, encerramento,
                desobrigacoes_plural, apoios_plural, confirmacoes_batismo_plural, apoio_membro_novo_plural, bencao_crianca_plural, ordenacoes_plural)
            SELECT @AlaId, tipo_template, nome, boas_vindas, desobrigacoes,
                apoios, confirmacoes_batismo, apoio_membro_novo, bencao_crianca, ordenacoes, sacramento,
                mensagens, live, encerramento,
                desobrigacoes_plural, apoios_plural, confirmacoes_batismo_plural, apoio_membro_novo_plural, bencao_crianca_plural, ordenacoes_plural
            FROM templates WHERE ala_id = 0",
            new { AlaId = alaId });
    }
}

public class UnidadeRepository(IDbContext db) : IUnidadeRepository
{
    public async Task<Unidade?> GetByAlaAsync(int alaId)
    {
        using var conn = db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<Unidade>(
            "SELECT * FROM unidades WHERE ala_id = @AlaId",
            new { AlaId = alaId });
    }

    public async Task UpsertAsync(Unidade u)
    {
        using var conn = db.CreateConnection();
        await conn.ExecuteAsync(@"
            INSERT INTO unidades (ala_id, nome, bispo, primeiro_conselheiro, segundo_conselheiro,
                                  estaca_id, horario, recepcionista, pianista, regente_musica,
                                  secretario_1, secretario_2, secretario_3, secretario_4)
            VALUES (@AlaId, @Nome, @Bispo, @PrimeiroConselheiro, @SegundoConselheiro,
                    @EstacaId, @Horario, @Recepcionista, @Pianista, @RegenteMusica,
                    @Secretario1, @Secretario2, @Secretario3, @Secretario4)
            ON CONFLICT(ala_id) DO UPDATE SET
                nome=excluded.nome, bispo=excluded.bispo,
                primeiro_conselheiro=excluded.primeiro_conselheiro,
                segundo_conselheiro=excluded.segundo_conselheiro,
                horario=excluded.horario, recepcionista=excluded.recepcionista,
                pianista=excluded.pianista, regente_musica=excluded.regente_musica,
                secretario_1=excluded.secretario_1, secretario_2=excluded.secretario_2,
                secretario_3=excluded.secretario_3, secretario_4=excluded.secretario_4", u);
    }
}

public class UserRepository(IDbContext db) : IUserRepository
{
    public async Task<User?> GetByUsernameAsync(string username)
    {
        using var conn = db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<User>(
            "SELECT * FROM users WHERE username = @Username",
            new { Username = username });
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        using var conn = db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<User>(
            "SELECT * FROM users WHERE id = @Id",
            new { Id = id });
    }

    public async Task<int> CreateAsync(User user)
    {
        using var conn = db.CreateConnection();
        return await conn.QuerySingleAsync<int>(@"
            INSERT INTO users (username, password, ala_id, role, display_name)
            VALUES (@Username, @Password, @AlaId, @Role, @DisplayName)
            RETURNING id", user);
    }

    public async Task<IEnumerable<User>> GetByAlaAsync(int alaId, string role)
    {
        using var conn = db.CreateConnection();
        var where = "ala_id = @AlaId AND username <> 'admin'";
        if (role is "conselheiro_1" or "conselheiro_2")
            where += " AND role IN ('conselheiro_1', 'conselheiro_2')";
        else if (role == "secretario")
            where += " AND role = 'secretario'";
        else if (role != "bispo")
            where += " AND 1 = 0";

        return await conn.QueryAsync<User>(
            $"SELECT * FROM users WHERE {where} ORDER BY role, id",
            new { AlaId = alaId });
    }

    public async Task<bool> UpdateDisplayNameAsync(int id, string? displayName)
    {
        using var conn = db.CreateConnection();
        var clean = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim();
        var rows = await conn.ExecuteAsync(
            "UPDATE users SET display_name = @DisplayName WHERE id = @Id",
            new { Id = id, DisplayName = clean });
        return rows > 0;
    }

    public async Task<bool> UpdatePasswordAsync(int id, string newPasswordHash)
    {
        using var conn = db.CreateConnection();
        var rows = await conn.ExecuteAsync(
            "UPDATE users SET password = @NewPasswordHash WHERE id = @Id",
            new { Id = id, NewPasswordHash = newPasswordHash });
        return rows > 0;
    }
}

public class AlaKeyRepository(IDbContext db) : IAlaKeyRepository
{
    public async Task<AlaKey?> GetByKeyAsync(string key)
    {
        using var conn = db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<AlaKey>(
            "SELECT * FROM ala_keys WHERE key = @Key",
            new { Key = key });
    }
}

public class TarefaRepository(IDbContext db) : ITarefaRepository
{
    public async Task<IEnumerable<Tarefa>> GetByAlaAsync(int alaId, string role)
    {
        using var conn = db.CreateConnection();
        var where = "ala_id = @AlaId";
        if (role is "conselheiro_1" or "conselheiro_2")
            where += " AND role IN ('conselheiro_1', 'conselheiro_2')";
        else if (role == "secretario")
            where += " AND role = 'secretario'";

        return await conn.QueryAsync<Tarefa>(
            $"SELECT * FROM tarefas WHERE {where} ORDER BY concluida ASC, data_prevista ASC NULLS LAST, criada_em DESC",
            new { AlaId = alaId });
    }

    public async Task<Tarefa?> GetByIdAsync(int id, int alaId)
    {
        using var conn = db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<Tarefa>(
            "SELECT * FROM tarefas WHERE id = @Id AND ala_id = @AlaId",
            new { Id = id, AlaId = alaId });
    }

    public async Task<int> CreateAsync(Tarefa t)
    {
        using var conn = db.CreateConnection();
        return await conn.QuerySingleAsync<int>(@"
            INSERT INTO tarefas (titulo, concluida, responsavel, data_prevista, criada_em, ala_id, role)
            VALUES (@Titulo, @Concluida, @Responsavel, @DataPrevista, @CriadaEm, @AlaId, @Role)
            RETURNING id", t);
    }

    public async Task UpdateAsync(Tarefa t)
    {
        using var conn = db.CreateConnection();
        await conn.ExecuteAsync(@"
            UPDATE tarefas SET
                titulo=@Titulo, concluida=@Concluida, responsavel=@Responsavel,
                data_prevista=@DataPrevista, concluida_em=@ConcluidaEm
            WHERE id=@Id AND ala_id=@AlaId", t);
    }

    public async Task DeleteAsync(int id, int alaId)
    {
        using var conn = db.CreateConnection();
        await conn.ExecuteAsync(
            "DELETE FROM tarefas WHERE id = @Id AND ala_id = @AlaId",
            new { Id = id, AlaId = alaId });
    }
}
