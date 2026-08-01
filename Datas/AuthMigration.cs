using Dapper;
using global::AtasApi.Data;

namespace AtasApi.Datas
{


    public class AuthMigration
    {
        private readonly IDbContext _db;
        public AuthMigration(IDbContext db) => _db = db;

        public void MigrateToBCrypt()
        {
            using var conn = _db.CreateConnection();

            // ANTIGAMENTE este método continha um dicionário com as senhas em
            // texto puro das contas legadas (ex.: "Criciuma_1", "Obra"). As
            // contas antigas já foram substituídas pelos logins por role
            // (EnsureRoleUsers) e todos os hashes atuais são BCrypt.
            //
            // Se ainda existirem hashes scrypt no banco, elas PRECISAM ser
            // resetadas manualmente (não é possível migrá-las sem as senhas
            // originais, que não devem mais existir em texto puro no código).
            var restantes = conn.QueryFirstOrDefault<int>(
                "SELECT COUNT(*) FROM users WHERE password LIKE 'scrypt:%'");

            if (restantes > 0)
            {
                Console.WriteLine($"[Migração] ATENÇÃO: {restantes} conta(s) ainda com hash scrypt antigo. " +
                    "Use o endpoint de troca de senha para redefinir a senha desses usuários.");
            }
            else
            {
                Console.WriteLine("[Migração] Nenhum hash scrypt restante. Todos os usuários já usam BCrypt.");
            }
        }
    }
}
