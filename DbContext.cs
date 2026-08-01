// ============================================================
// Data/DbContext.cs
// Provedor de conexão SQLite usando Dapper (sem EF Core).
// Se preferir EF Core, veja o comentário alternativo ao final.
// ============================================================
using System.Data;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;

namespace AtasApi.Data;

public interface IDbContext
{
    IDbConnection CreateConnection();
}

public class SqliteDbContext : IDbContext
{
    private readonly string _connectionString;

    public SqliteDbContext(string connectionString)
    {
        _connectionString = connectionString;

        var sqliteBuilder = new SqliteConnectionStringBuilder(connectionString);
        var dataSource = sqliteBuilder.DataSource;

        if (!string.IsNullOrWhiteSpace(dataSource))
        {
            var directory = Path.GetDirectoryName(Path.GetFullPath(dataSource));
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);
        }
    }

    public IDbConnection CreateConnection()
    {
        var conn = new SqliteConnection(_connectionString);

        // Habilita WAL e foreign keys assim que a conexão abre
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "PRAGMA journal_mode=WAL; PRAGMA foreign_keys=ON;";
        cmd.ExecuteNonQuery();

        return conn;
    }
}

// ──────────────────────────────────────────
// Data/DatabaseInitializer.cs
// Aplica o schema na primeira execução (equivalente ao init_db() do Python)
// ──────────────────────────────────────────

/// <summary>
/// Executa o script SQL inicial se as tabelas obrigatórias ainda não existirem.
/// Registre como IHostedService ou chame em Program.cs antes de app.Run().
/// </summary>
public class DatabaseInitializer
{
    private readonly IDbContext _db;
    private readonly string _schemaPath;

    public DatabaseInitializer(IDbContext db, string schemaPath)
    {
        _db = db;
        _schemaPath = schemaPath;
    }

    public void Initialize()
    {
        using var conn = _db.CreateConnection();

        // Verifica se as tabelas obrigatórias existem
        const string checkSql = @"
            SELECT COUNT(*) FROM sqlite_master
            WHERE type='table' AND name IN ('atas','sacramental','users');";

        using var cmd = (conn as SqliteConnection)!.CreateCommand();
        cmd.CommandText = checkSql;
        var count = Convert.ToInt32(cmd.ExecuteScalar());

        if (count < 3)
        {
            var schema = File.ReadAllText(_schemaPath);
            cmd.CommandText = schema;
            cmd.ExecuteNonQuery();
            Console.WriteLine("[DbInit] Schema aplicado com sucesso.");
        }

        // Garante colunas adicionais de migrações (equivalente ao ensure_sacramental_columns)
        EnsureSacramentalColumns(conn as SqliteConnection);

        // Garante colunas dos templates (ordenações)
        EnsureTemplateColumns(conn as SqliteConnection);

        // Garante UNIQUE em ala_id na tabela unidades (permite ON CONFLICT no upsert)
        EnsureUnidadeUniqueAlaId(conn as SqliteConnection);

        // Garante tabela tarefas e colunas secretário
        EnsureTarefasTable(conn as SqliteConnection);
        EnsureTarefaRoleColumn(conn as SqliteConnection);
        EnsureSecretarioColumns(conn as SqliteConnection);

        // Garante colunas de auth e tabela ala_keys
        EnsureAuthColumns(conn as SqliteConnection);
        EnsureAlaKeysTable(conn as SqliteConnection);

        // Substitui contas antigas (por ala) por logins com roles
        EnsureRoleUsers(conn as SqliteConnection);

        // Status das atas sacramentais: concluída só com os 2 discursantes preenchidos
        RecalcularStatusSacramentais(conn as SqliteConnection);
    }

    private static void RecalcularStatusSacramentais(SqliteConnection? conn)
    {
        if (conn is null) return;

        try
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE atas
                SET status = CASE
                    WHEN EXISTS (
                        SELECT 1 FROM sacramental s
                        WHERE s.ata_id = atas.id
                          AND TRIM(COALESCE(s.discursante_1, '')) <> ''
                          AND TRIM(COALESCE(s.discursante_2, '')) <> ''
                    ) THEN 'concluida'
                    ELSE 'rascunho'
                END
                WHERE tipo = 'sacramental'";
            var changed = cmd.ExecuteNonQuery();
            if (changed > 0)
                Console.WriteLine($"[DbInit] Status de atas sacramentais recalculado ({changed} atualizada(s)).");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DbInit] Não foi possível recalcular status das atas: {ex.Message}");
        }
    }



    private static void EnsureSacramentalColumns(SqliteConnection? conn)
    {
        if (conn is null) return;

        var columnsToAdd = new Dictionary<string, string>
        {
            { "discursante_1", "TEXT" },
            { "discursante_2", "TEXT" },
            { "outros",        "TEXT" },
            { "tema_1",        "TEXT" },
            { "tema_2",        "TEXT" },
            { "tema_ultimo",   "TEXT" },
            { "obs_1",         "TEXT" },
            { "obs_2",         "TEXT" },
            { "obs_ultimo",    "TEXT" },
            { "testemunhos",   "TEXT" }
        };

        using var pragmaCmd = conn.CreateCommand();
        pragmaCmd.CommandText = "PRAGMA table_info(sacramental)";
        var existingColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        using (var reader = pragmaCmd.ExecuteReader())
        {
            while (reader.Read())
                existingColumns.Add(reader["name"].ToString()!);
        }

        foreach (var (col, type) in columnsToAdd)
        {
            if (!existingColumns.Contains(col))
            {
                try
                {
                    using var alterCmd = conn.CreateCommand();
                    alterCmd.CommandText = $"ALTER TABLE sacramental ADD COLUMN {col} {type}";
                    alterCmd.ExecuteNonQuery();
                    Console.WriteLine($"[DbInit] Coluna '{col}' adicionada em sacramental.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DbInit] Não foi possível adicionar coluna '{col}': {ex.Message}");
                }
            }
        }
    }

    private static void EnsureTemplateColumns(SqliteConnection? conn)
    {
        if (conn is null) return;

        using var pragmaCmd = conn.CreateCommand();
        pragmaCmd.CommandText = "PRAGMA table_info(templates)";
        var existingColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        using (var reader = pragmaCmd.ExecuteReader())
        {
            while (reader.Read())
                existingColumns.Add(reader["name"].ToString()!);
        }

        // Singular (ordenações) + versões plurais de todos os assuntos da ala
        var columnsToAdd = new Dictionary<string, string>
        {
            { "ordenacoes", "TEXT" },
            { "desobrigacoes_plural", "TEXT" },
            { "apoios_plural", "TEXT" },
            { "confirmacoes_batismo_plural", "TEXT" },
            { "apoio_membro_novo_plural", "TEXT" },
            { "bencao_crianca_plural", "TEXT" },
            { "ordenacoes_plural", "TEXT" }
        };

        var novos = columnsToAdd.Keys.Where(c => !existingColumns.Contains(c)).ToList();
        if (novos.Count == 0) return;

        try
        {
            foreach (var col in novos)
            {
                using var alterCmd = conn.CreateCommand();
                alterCmd.CommandText = $"ALTER TABLE templates ADD COLUMN {col} TEXT NOT NULL DEFAULT ''";
                alterCmd.ExecuteNonQuery();
            }

            using var updateCmd = conn.CreateCommand();
            updateCmd.CommandText = @"
                UPDATE templates SET
                    ordenacoes = CASE WHEN ordenacoes = '' THEN 'É proposto que [NOME] receba o Sacerdócio de Melquisedeque e seja ordenado(a) como [CHAMADO]. Os que forem a favor, manifestem-se levantando a mão. [Pequena pausa.] Os que se opuserem, se houver, manifestem-se.' ELSE ordenacoes END,
                    desobrigacoes_plural = '[LISTA] Os que desejarem manifestar agradecimento por seus serviços prestados podem fazê-lo levantando a mão.',
                    apoios_plural = '[LISTA] Os que forem a favor, manifestem-se levantando a mão. [Pequena pausa.] Os que se opuserem, se houver, manifestem-se. [Pequena pausa.]',
                    confirmacoes_batismo_plural = 'Os irmãos [LISTA] foram batizados, gostaríamos de convidá-los para vir até o púlpito para que possamos fazer sua confirmação como membros de A Igreja de Jesus Cristo dos Santos dos Últimos Dias.',
                    apoio_membro_novo_plural = 'Os irmãos [LISTA] foram batizados e confirmados membros da igreja, e gostaríamos do apoio de todos os irmãos de plena aceitação como novos membros da ala. Todos a favor, manifestem-se.',
                    bencao_crianca_plural = 'Gostaríamos de chamar ao púlpito os irmãos que irão dar a bênção de apresentação das crianças [LISTA].',
                    ordenacoes_plural = '[LISTA] Os que forem a favor, manifestem-se levantando a mão. [Pequena pausa.] Os que se opuserem, se houver, manifestem-se.'";
            updateCmd.ExecuteNonQuery();
            Console.WriteLine($"[DbInit] Colunas adicionadas em templates: {string.Join(", ", novos)}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DbInit] Não foi possível adicionar colunas em templates: {ex.Message}");
        }
    }

    private static void EnsureUnidadeUniqueAlaId(SqliteConnection? conn)
    {
        if (conn is null) return;

        // Verifica se já existe unique index
        using var checkCmd = conn.CreateCommand();
        checkCmd.CommandText = @"
            SELECT COUNT(*) FROM sqlite_master
            WHERE type='index' AND tbl_name='unidades' AND ""unique""=1 AND sql LIKE '%ala_id%'";
        var hasUnique = Convert.ToInt32(checkCmd.ExecuteScalar()) > 0;

        if (hasUnique)
        {
            Console.WriteLine("[DbInit] UNIQUE index em unidades(ala_id) já existe.");
            return;
        }

        Console.WriteLine("[DbInit] Criando UNIQUE index em unidades(ala_id)...");

        // 1) Remove duplicatas: mantém apenas a linha com ID mais baixo por ala_id
        using var dedupCmd = conn.CreateCommand();
        dedupCmd.CommandText = @"
            DELETE FROM unidades
            WHERE id NOT IN (
                SELECT MIN(id) FROM unidades GROUP BY ala_id
            )";
        var removed = dedupCmd.ExecuteNonQuery();
        if (removed > 0)
            Console.WriteLine($"[DbInit] {removed} linhas duplicadas removidas de unidades.");

        // 2) Cria o unique index
        using var createIdx = conn.CreateCommand();
        createIdx.CommandText = "CREATE UNIQUE INDEX IF NOT EXISTS idx_unidades_ala_id_unique ON unidades(ala_id)";
        createIdx.ExecuteNonQuery();

        Console.WriteLine("[DbInit] UNIQUE index em unidades(ala_id) criado com sucesso.");
    }

    private static void EnsureTarefasTable(SqliteConnection? conn)
    {
        if (conn is null) return;

        using var checkCmd = conn.CreateCommand();
        checkCmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='tarefas'";
        if (Convert.ToInt32(checkCmd.ExecuteScalar()) > 0) return;

        using var createCmd = conn.CreateCommand();
        createCmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS tarefas (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                titulo TEXT NOT NULL,
                concluida INTEGER NOT NULL DEFAULT 0,
                responsavel TEXT,
                data_prevista TEXT,
                concluida_em TEXT,
                criada_em TEXT NOT NULL DEFAULT (datetime('now')),
                ala_id INTEGER NOT NULL,
                role TEXT NOT NULL DEFAULT '',
                FOREIGN KEY(ala_id) REFERENCES users(id)
            );
            CREATE INDEX IF NOT EXISTS idx_tarefas_ala_id ON tarefas(ala_id);
            CREATE INDEX IF NOT EXISTS idx_tarefas_concluida ON tarefas(concluida);";
        createCmd.ExecuteNonQuery();
        Console.WriteLine("[DbInit] Tabela 'tarefas' criada com sucesso.");
    }

    private static void EnsureTarefaRoleColumn(SqliteConnection? conn)
    {
        if (conn is null) return;

        using var pragmaCmd = conn.CreateCommand();
        pragmaCmd.CommandText = "PRAGMA table_info(tarefas)";
        var existingColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using (var reader = pragmaCmd.ExecuteReader())
        {
            while (reader.Read())
                existingColumns.Add(reader["name"].ToString()!);
        }

        if (!existingColumns.Contains("role"))
        {
            try
            {
                using var alterCmd = conn.CreateCommand();
                alterCmd.CommandText = "ALTER TABLE tarefas ADD COLUMN role TEXT NOT NULL DEFAULT ''";
                alterCmd.ExecuteNonQuery();
                Console.WriteLine("[DbInit] Coluna 'role' adicionada em tarefas.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DbInit] Não foi possível adicionar coluna 'role' em tarefas: {ex.Message}");
            }
        }
    }

    private static void EnsureSecretarioColumns(SqliteConnection? conn)
    {
        if (conn is null) return;

        using var pragmaCmd = conn.CreateCommand();
        pragmaCmd.CommandText = "PRAGMA table_info(unidades)";
        var existingColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using (var reader = pragmaCmd.ExecuteReader())
        {
            while (reader.Read())
                existingColumns.Add(reader["name"].ToString()!);
        }

        foreach (var col in new[] { "secretario_1", "secretario_2", "secretario_3", "secretario_4" })
        {
            if (!existingColumns.Contains(col))
            {
                try
                {
                    using var alterCmd = conn.CreateCommand();
                    alterCmd.CommandText = $"ALTER TABLE unidades ADD COLUMN {col} TEXT";
                    alterCmd.ExecuteNonQuery();
                    Console.WriteLine($"[DbInit] Coluna '{col}' adicionada em unidades.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DbInit] Não foi possível adicionar coluna '{col}': {ex.Message}");
                }
            }
        }
    }

    private static void EnsureAuthColumns(SqliteConnection? conn)
    {
        if (conn is null) return;

        using var pragmaCmd = conn.CreateCommand();
        pragmaCmd.CommandText = "PRAGMA table_info(users)";
        var existingColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using (var reader = pragmaCmd.ExecuteReader())
        {
            while (reader.Read())
                existingColumns.Add(reader["name"].ToString()!);
        }

        foreach (var (col, type) in new Dictionary<string, string>
        {
            { "ala_id", "INTEGER DEFAULT 1" },
            { "role", "TEXT DEFAULT 'bispo'" },
            { "display_name", "TEXT" }
        })
        {
            if (!existingColumns.Contains(col))
            {
                try
                {
                    using var alterCmd = conn.CreateCommand();
                    alterCmd.CommandText = $"ALTER TABLE users ADD COLUMN {col} {type}";
                    alterCmd.ExecuteNonQuery();
                    Console.WriteLine($"[DbInit] Coluna '{col}' adicionada em users.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DbInit] Não foi possível adicionar coluna '{col}': {ex.Message}");
                }
            }
        }

        using var updateCmd = conn.CreateCommand();
        updateCmd.CommandText = "UPDATE users SET ala_id = id WHERE ala_id IS NULL";
        updateCmd.ExecuteNonQuery();

        using var indexCmd = conn.CreateCommand();
        indexCmd.CommandText = "CREATE INDEX IF NOT EXISTS idx_users_ala_id ON users(ala_id)";
        indexCmd.ExecuteNonQuery();
    }

    private static void EnsureAlaKeysTable(SqliteConnection? conn)
    {
        if (conn is null) return;

        using var checkCmd = conn.CreateCommand();
        checkCmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='ala_keys'";
        if (Convert.ToInt32(checkCmd.ExecuteScalar()) > 0) return;

        using var createCmd = conn.CreateCommand();
        createCmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS ala_keys (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                key TEXT NOT NULL UNIQUE,
                ala_id INTEGER NOT NULL,
                role TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS idx_ala_keys_key ON ala_keys(key);";
        createCmd.ExecuteNonQuery();

        // Chaves de convite aleatórias (nada de valores fixos/adivinháveis no código).
        var alas = new (int Id, string Nome)[]
        {
            (1, "Ala Criciúma 1"), (2, "Ala Criciúma 2"), (3, "Ala Criciúma 3"),
            (4, "Ala Içara"), (5, "Ala Araranguá"), (6, "Obra Unidade")
        };

        Console.WriteLine("[DbInit] Chaves de convite geradas (guardar com segurança):");
        foreach (var ala in alas)
        {
            foreach (var (role, cargo) in new (string, string)[]
            {
                ("bispo", "Bispo"),
                ("conselheiro_1", "1º Conselheiro"),
                ("conselheiro_2", "2º Conselheiro"),
                ("secretario", "Secretário")
            })
            {
                var key = RandomString(10, "ABCDEFGHJKMNPQRSTUVWXYZ23456789");
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    INSERT OR IGNORE INTO ala_keys (key, ala_id, role)
                    VALUES (@Key, @AlaId, @Role)";
                cmd.Parameters.AddWithValue("@Key", key);
                cmd.Parameters.AddWithValue("@AlaId", ala.Id);
                cmd.Parameters.AddWithValue("@Role", role);
                cmd.ExecuteNonQuery();

                Console.WriteLine($"[DbInit]   {ala.Nome} / {cargo}: {key}");
            }
        }
        Console.WriteLine("[DbInit] Tabela 'ala_keys' criada com chaves de convite aleatórias.");
    }

    /// <summary>
    /// Remove as contas antigas (uma por ala) e cria logins por role:
    /// 1 bispo, 2 conselheiros e 3 secretários para cada ala.
    /// O bispo reutiliza os ids 1..6 das alas para preservar as referências
    /// (atas, unidades, tarefas, templates). A conta 'admin' é mantida.
    /// As senhas são aleatórias e exibidas UMA ÚNICA VEZ no console de inicialização.
    /// </summary>
    private static void EnsureRoleUsers(SqliteConnection? conn)
    {
        if (conn is null) return;

        // Idempotência: se os logins por role já existem, não refaz.
        using (var checkCmd = conn.CreateCommand())
        {
            checkCmd.CommandText = "SELECT COUNT(*) FROM users WHERE username = 'criciuma1_bispo'";
            if (Convert.ToInt32(checkCmd.ExecuteScalar()) > 0)
            {
                Console.WriteLine("[DbInit] Logins por role já existem. Pulando criação.");
                return;
            }
        }

        // 1) Remove as contas antigas por ala (mantém 'admin')
        Console.WriteLine("[DbInit] Removendo contas antigas por ala...");
        using (var fkCmd = conn.CreateCommand())
        {
            fkCmd.CommandText = "PRAGMA foreign_keys = OFF";
            fkCmd.ExecuteNonQuery();
        }
        using (var delCmd = conn.CreateCommand())
        {
            delCmd.CommandText = "DELETE FROM users WHERE username IN ('Criciuma_1','Criciuma_2','Criciuma_3','Içara','Ararangua','Obra')";
            delCmd.ExecuteNonQuery();
        }

        // 2) Cria os logins por role
        var alas = new (int Id, string Slug, string Nome)[]
        {
            (1, "criciuma1", "Ala Criciúma 1"),
            (2, "criciuma2", "Ala Criciúma 2"),
            (3, "criciuma3", "Ala Criciúma 3"),
            (4, "icara", "Ala Içara"),
            (5, "ararangua", "Ala Araranguá"),
            (6, "obra", "Obra Unidade")
        };

        var roles = new (string Sufixo, string Role, string Display, string Cargo)[]
        {
            ("bispo", "bispo", "Bispo", "Bispo"),
            ("conselheiro_1", "conselheiro_1", "1º Conselheiro", "1º Conselheiro"),
            ("conselheiro_2", "conselheiro_2", "2º Conselheiro", "2º Conselheiro"),
            ("secretario_1", "secretario", "Secretário 1", "Secretário 1"),
            ("secretario_2", "secretario", "Secretário 2", "Secretário 2"),
            ("secretario_3", "secretario", "Secretário 3", "Secretário 3"),
        };

        Console.WriteLine("[DbInit] Credenciais iniciais geradas (NUNCA versionar!):");
        // 1ª fase: bispos com id fixo (1..6) para preservar as referências de ala.
        // CRÍTICO: bispos PRIMEIRO, senão os autoincrements dos demais cargos
        // ocupam os ids 1..6 e o INSERT OR IGNORE do bispo é silenciosamente descartado.
        foreach (var ala in alas)
        {
            CriarUsuarioRole(conn, ala.Id, $"{ala.Slug}_bispo", ala.Nome, "bispo", "Bispo", usarIdFixo: true);
        }
        // 2ª fase: conselheiros e secretários (autoincrement).
        foreach (var ala in alas)
        {
            foreach (var r in roles.Skip(1))
            {
                CriarUsuarioRole(conn, ala.Id, $"{ala.Slug}_{r.Sufixo}", ala.Nome, r.Role, r.Display, usarIdFixo: false);
            }
        }

        Console.WriteLine("[DbInit] Logins por role criados (1 bispo, 2 conselheiros, 3 secretários por ala).");
    }

    private static void CriarUsuarioRole(SqliteConnection conn, int alaId, string username, string alaNome,
        string role, string display, bool usarIdFixo)
    {
        var password = RandomPassword(16);
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

        using var cmd = conn.CreateCommand();
        if (usarIdFixo)
        {
            cmd.CommandText = @"
                INSERT OR IGNORE INTO users (id, username, password, ala_id, role, display_name)
                VALUES (@id, @username, @password, @alaId, @role, @display)";
            cmd.Parameters.AddWithValue("@id", alaId);
        }
        else
        {
            cmd.CommandText = @"
                INSERT OR IGNORE INTO users (username, password, ala_id, role, display_name)
                VALUES (@username, @password, @alaId, @role, @display)";
        }
        cmd.Parameters.AddWithValue("@username", username);
        cmd.Parameters.AddWithValue("@password", passwordHash);
        cmd.Parameters.AddWithValue("@alaId", alaId);
        cmd.Parameters.AddWithValue("@role", role);
        cmd.Parameters.AddWithValue("@display", display);
        cmd.ExecuteNonQuery();

        Console.WriteLine($"[DbInit]   {username}  ({alaNome} / {display}): {password}");
    }

    private static string RandomPassword(int length)
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789@#$%&*!?";
        return RandomString(length, chars);
    }

    private static string RandomString(int length, string charset)
    {
        var bytes = new byte[length];
        RandomNumberGenerator.Fill(bytes);
        var sb = new StringBuilder(length);
        foreach (var b in bytes)
            sb.Append(charset[b % charset.Length]);
        return sb.ToString();
    }
}

/*
 * ──────────────────────────────────────────────────────
 * ALTERNATIVA: EF Core com SQLite
 * Se preferir EF Core ao Dapper, substitua o IDbContext
 * por um DbContext assim:
 *
 *   public class AtasDbContext : DbContext
 *   {
 *       public DbSet<User> Users => Set<User>();
 *       public DbSet<Ata> Atas => Set<Ata>();
 *       public DbSet<Sacramental> Sacramentais => Set<Sacramental>();
 *       public DbSet<Batismo> Batismos => Set<Batismo>();
 *       public DbSet<Unidade> Unidades => Set<Unidade>();
 *       public DbSet<Template> Templates => Set<Template>();
 *       public DbSet<Estaca> Estacas => Set<Estaca>();
 *
 *       protected override void OnModelCreating(ModelBuilder b)
 *       {
 *           b.Entity<Ata>().HasIndex(a => a.AlaId);
 *           b.Entity<Ata>().HasIndex(a => a.Data);
 *           // ... índices conforme schema_inicial.sql
 *       }
 *   }
 *
 * E registre em Program.cs:
 *   builder.Services.AddDbContext<AtasDbContext>(o =>
 *       o.UseSqlite(connectionString));
 * ──────────────────────────────────────────────────────
 */
