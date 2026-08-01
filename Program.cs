using AtasApi.Configuration;
using AtasApi.Data;
using AtasApi.Hinos;
using AtasApi.Middleware;
using AtasApi.Repositories;
using AtasApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HostFiltering;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Threading.RateLimiting;

Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

// â”€â”€ ConfiguraÃ§Ãµes â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
var settings = builder.Configuration.Get<AppSettings>()!;

builder.Services.Configure<HostFilteringOptions>(options =>
{
    var allowedHosts = settings.AllowedHosts?.Length > 0
        ? settings.AllowedHosts
        : ["*"];

    options.AllowedHosts = allowedHosts;
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
        | ForwardedHeaders.XForwardedProto
        | ForwardedHeaders.XForwardedHost;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// â”€â”€ SeguranÃ§a do JWT: segredo obrigatÃ³rio e forte â”€â”€â”€â”€â”€â”€â”€â”€â”€
// O segredo NUNCA deve ficar no appsettings.json versionado. Defina
// Jwt__Secret (variÃ¡vel de ambiente) ou appsettings.Development.json
// / appsettings.Production.json (ambos ignorados pelo .gitignore).
var jwtSecret = settings.Jwt.Secret;
if (string.IsNullOrWhiteSpace(jwtSecret)
    || jwtSecret.Length < 32
    || jwtSecret == "TROQUE_POR_UMA_CHAVE_SECRETA_DE_32_CARACTERES_MINIMO")
{
    throw new InvalidOperationException(
        "[SEGURANÃ‡A] JWT Secret ausente ou fraco. Defina a variÃ¡vel de ambiente " +
        "Jwt__Secret (ou appsettings.Development.json / appsettings.Production.json, " +
        "ambos ignorados pelo git) com uma chave aleatÃ³ria de 32+ caracteres. " +
        "NÃƒO coloque o segredo em appsettings.json versionado.");
}

// â”€â”€ Banco de dados â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
builder.Services.AddSingleton<IDbContext>(new SqliteDbContext(settings.ConnectionString));

// â”€â”€ RepositÃ³rios e ServiÃ§os â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
builder.Services.AddScoped<IAtaRepository, AtaRepository>();
builder.Services.AddScoped<ISacramentalRepository, SacramentalRepository>();
builder.Services.AddScoped<IBatismoRepository, BatismoRepository>();
builder.Services.AddScoped<ITemplateRepository, TemplateRepository>();
builder.Services.AddScoped<IUnidadeRepository, UnidadeRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAlaKeyRepository, AlaKeyRepository>();
builder.Services.AddScoped<ITarefaRepository, TarefaRepository>();

builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<IAtaService, AtaService>();
builder.Services.AddScoped<ISacramentalService, SacramentalService>();
builder.Services.AddScoped<IBatismoService, BatismoService>();
builder.Services.AddScoped<ITemplateService, TemplateService>();
builder.Services.AddScoped<IUnidadeService, UnidadeService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITarefaService, TarefaService>();
builder.Services.AddSingleton<IJwtService, JwtService>();

// â”€â”€ Hinos â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
builder.Services.AddHttpClient<IHinoScraperService, HinoScraperService>();
builder.Services.AddScoped<IHinoService, HinoService>();

// â”€â”€ JWT e Auth â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Jwt.Secret)),
            ValidateIssuer = true,
            ValidIssuer = settings.Jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = settings.Jwt.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        // O DETETIVE: Isso vai cuspir o motivo exato do erro 401 no console
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine("\n##########################################");
                Console.WriteLine("[AUTH FAILED] MOTIVO DA REJEIÃ‡ÃƒO DO TOKEN:");
                Console.WriteLine(context.Exception.Message);
                Console.WriteLine("##########################################\n");
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                // Se o token nem sequer chegou ou estava num formato invÃ¡lido
                if (context.AuthenticateFailure != null)
                {
                    Console.WriteLine($"[AUTH CHALLENGE] Falha: {context.AuthenticateFailure.Message}");
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// â”€â”€ Rate Limiting â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
builder.Services.AddRateLimiter(rateLimiter =>
{
    rateLimiter.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "anon", _ =>
            new FixedWindowRateLimiterOptions { Window = TimeSpan.FromHours(1), PermitLimit = settings.RateLimit.DefaultPerHour }));

    rateLimiter.AddFixedWindowLimiter("login", options => { options.Window = TimeSpan.FromMinutes(1); options.PermitLimit = settings.RateLimit.LoginAttemptsPerMinute; });
    rateLimiter.AddFixedWindowLimiter("register", options => { options.Window = TimeSpan.FromMinutes(1); options.PermitLimit = settings.RateLimit.RegisterAttemptsPerMinute; });
});

var allowedOrigins = settings.AllowedOrigins?.Length > 0
    ? settings.AllowedOrigins
    : new[] { "http://localhost:4200", "https://localhost:4200", "http://127.0.0.1:4200" };
builder.Services.AddCors(o => o.AddPolicy("frontend", p =>
    p.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddControllers();

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "AtasApi", Version = "v1" });

    // ConfiguraÃ§Ã£o do botÃ£o verde "Authorize"
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http, // <- ISSO AQUI FAZ A MÃGICA
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Insira APENAS o token JWT abaixo (nÃ£o precisa escrever 'Bearer')."
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();
try
{
    var dbContext = app.Services.GetRequiredService<IDbContext>();

    var initializer = new DatabaseInitializer(dbContext, settings.SchemaPath);
    initializer.Initialize();

    var authMigrator = new AtasApi.Datas.AuthMigration(dbContext);
    authMigrator.MigrateToBCrypt();
}
catch (Exception ex)
{
    // Isso vai imprimir o erro real no seu console antes de fechar
    Console.WriteLine("##########################################");
    Console.WriteLine("ERRO FATAL NA INICIALIZAÃ‡ÃƒO DO BANCO:");
    Console.WriteLine(ex.ToString());
    Console.WriteLine("##########################################");
    throw;
}

app.UseForwardedHeaders();
app.UseHostFiltering();

app.UseForwardedHeaders();
app.UseHostFiltering();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRateLimiter();
app.UseCors("frontend");
app.UseAuthentication();
app.UseAuthorization();

// â”€â”€ Headers de seguranÃ§a â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
app.Use(async (ctx, next) =>
{
    var headers = ctx.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "DENY";
    headers["Referrer-Policy"] = "no-referrer";
    headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    if (!app.Environment.IsDevelopment())
        headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
    await next();
});

app.Use(async (ctx, next) => {
    ctx.Response.Headers.CacheControl = "no-store, no-cache, must-revalidate, max-age=0";
    await next();
});

app.MapControllers();
app.Run();
public partial class Program { }
