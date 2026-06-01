using System.Text;
using System.Threading.RateLimiting;
using AspireApp1.Server;
using AspireApp1.Server.Auditing;
using AspireApp1.Server.Configuration;
using AspireApp1.Server.Hubs;
using AspireApp1.Server.Models;
using AspireApp1.Server.Repositories;
using AspireApp1.Server.Security;
using AspireApp1.Server.Services;
using AspireApp1.Server.Services.Auth;
using AspireApp1.Server.Services.Auth.Mfa;
using AspireApp1.Server.Services.Email;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;


WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddHttpContextAccessor();

// DbContext registered via factory so the action logger can spin up its own
// short-lived contexts for fire-and-forget audit writes without colliding with
// the request-scoped DbContext shared by controllers and repositories.
builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<AppDbContext>(sp =>
    sp.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext());

// ── Identity ─────────────────────────────────────────────────────────────────
builder.Services
    .AddIdentity<User, Role>(opts =>
    {
        // Match what the frontend register form already advertises: 7 chars,
        // letter + digit. Uppercase / symbols not required — keeps the academic
        // demo credentials usable without surprising the user.
        opts.Password.RequiredLength = 7;
        opts.Password.RequireDigit = true;
        opts.Password.RequireLowercase = true;
        opts.Password.RequireUppercase = false;
        opts.Password.RequireNonAlphanumeric = false;

        opts.User.RequireUniqueEmail = true;
        opts.SignIn.RequireConfirmedEmail = true;

        opts.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
        opts.Lockout.MaxFailedAccessAttempts = 5;
        opts.Lockout.AllowedForNewUsers = true;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// ── SMTP email ───────────────────────────────────────────────────────────────
builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection(SmtpOptions.SectionName));

// ── JWT auth ─────────────────────────────────────────────────────────────────
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
JwtOptions jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
                       ?? throw new InvalidOperationException("Missing Jwt configuration section");

byte[] signingBytes = Encoding.UTF8.GetBytes(jwtOptions.SigningKey);

// AddIdentity (above) silently sets DefaultAuthenticateScheme /
// DefaultChallengeScheme / DefaultSignInScheme to its cookie schemes. Those
// more-specific defaults override what AddAuthentication(string) sets, so we
// have to set them explicitly — otherwise [Authorize] runs cookie auth, finds
// no cookie, and 401s with no JwtBearer events ever firing.
builder.Services
    .AddAuthentication(opts =>
    {
        opts.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        opts.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        opts.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        opts.DefaultForbidScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(opts =>
    {
        opts.SaveToken = true;
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(signingBytes),
            ClockSkew = TimeSpan.Zero,
        };

        opts.Events = new JwtBearerEvents
        {
            // Reject MFA challenge tokens from accessing protected endpoints —
            // they exist only to authorize the /login/2fa step.
            OnTokenValidated = ctx =>
            {
                string? purpose = ctx.Principal?.FindFirst(JwtTokenService.PurposeClaim)?.Value;
                if (purpose != JwtTokenService.AccessPurpose)
                {
                    ctx.Fail("Token is not an access token.");
                }
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = ctx =>
            {
                ILogger logger = ctx.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("JwtBearer");
                logger.LogWarning(ctx.Exception,
                    "JWT auth failed for {Path}: {Message}",
                    ctx.HttpContext.Request.Path, ctx.Exception.Message);
                return Task.CompletedTask;
            },
            OnChallenge = ctx =>
            {
                ILogger logger = ctx.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("JwtBearer");
                logger.LogWarning(
                    "JWT challenge for {Path}: error={Error} description={Description}",
                    ctx.HttpContext.Request.Path, ctx.Error, ctx.ErrorDescription);
                return Task.CompletedTask;
            },
            // Lift access tokens from the query string only when hitting the
            // SignalR hubs — WebSocket upgrade requests can't carry auth headers.
            OnMessageReceived = ctx =>
            {
                string? accessToken = ctx.Request.Query["access_token"];
                PathString path = ctx.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    ctx.Token = accessToken;
                }
                return Task.CompletedTask;
            },
        };
    });

builder.Services.AddAuthorization(opts =>
{
    opts.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
    opts.AddPolicy("CustomerOnly", p => p.RequireRole("Customer"));

    // Permission-based policies — one per permission code. Names map 1:1 to
    // the codes seeded in AppDbContext.
    string[] permissionCodes =
    {
        "desserts:read", "desserts:create", "desserts:update", "desserts:delete",
        "orders:read:all", "orders:read:own", "orders:create",
        "orders:archive", "orders:delete", "statistics:view",
    };
    foreach (string code in permissionCodes)
    {
        opts.AddPolicy($"perm:{code}", p =>
            p.RequireAssertion(ctx =>
                ctx.User.HasClaim(c => c.Type == JwtTokenService.PermissionsClaim && c.Value == code)));
    }
});

// ── Auth services ────────────────────────────────────────────────────────────
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IRefreshTokenStore, RefreshTokenStore>();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<IMfaFactor, EmailOtpFactor>();
builder.Services.AddScoped<IMfaFactor, TotpAuthenticatorFactor>();
builder.Services.AddScoped<IMfaChallengePipeline, MfaChallengePipeline>();

// ── Auditing (action log) ────────────────────────────────────────────────────
builder.Services.AddScoped<ICurrentUserContext, CurrentUserContext>();
builder.Services.AddScoped<IActionLogger, ActionLogger>();

// Repositories: register concrete first, then expose the interface via a decorator
// that wraps every method call with an action-log entry.
builder.Services.AddScoped<DessertRepository>();
builder.Services.AddScoped<IDessertRepository>(sp =>
    new LoggingDessertRepository(
        sp.GetRequiredService<DessertRepository>(),
        sp.GetRequiredService<IActionLogger>()));
builder.Services.AddScoped<DessertService>();

builder.Services.AddScoped<OrderRepository>();
builder.Services.AddScoped<IOrderRepository>(sp =>
    new LoggingOrderRepository(
        sp.GetRequiredService<OrderRepository>(),
        sp.GetRequiredService<IActionLogger>()));
builder.Services.AddScoped<OrderService>();

builder.Services.AddScoped<StatisticsRepository>();
builder.Services.AddScoped<IStatisticsRepository>(sp =>
    new LoggingStatisticsRepository(
        sp.GetRequiredService<StatisticsRepository>(),
        sp.GetRequiredService<IActionLogger>()));
builder.Services.AddScoped<StatisticsService>();

builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<IUserRepository>(sp =>
    new LoggingUserRepository(
        sp.GetRequiredService<UserRepository>(),
        sp.GetRequiredService<IActionLogger>()));

builder.Services.AddScoped<RoleRepository>();
builder.Services.AddScoped<IRoleRepository>(sp =>
    new LoggingRoleRepository(
        sp.GetRequiredService<RoleRepository>(),
        sp.GetRequiredService<IActionLogger>()));

builder.Services.AddScoped<AuthService>();

// ── Security (threat detection) ──────────────────────────────────────────────
builder.Services.Configure<SecurityOptions>(
    builder.Configuration.GetSection(SecurityOptions.SectionName));
builder.Services.AddScoped<IThreatRule, FailedLoginBurstRule>();
builder.Services.AddScoped<IThreatRule, OrderBurstRule>();
builder.Services.AddScoped<IThreatRule, ChatBurstRule>();
builder.Services.AddHostedService<ThreatDetectorWorker>();
builder.Services.AddScoped<IUserBlocklist, UserBlocklist>();

builder.Services.Configure<MongoOptions>(
    builder.Configuration.GetSection(MongoOptions.SectionName));
var mongoConn = builder.Configuration["Mongo:ConnectionString"];
Console.WriteLine($"MONGO CONNECTION: {mongoConn}");
// Inner mongo-backed repo stays a singleton; the audit-logging decorator is
// scoped so it can depend on the scoped IActionLogger. SignalR hubs resolve
// scoped services per invocation, so chat sends pick up the decorator.
builder.Services.AddSingleton<ChatRepository>();
builder.Services.AddScoped<IChatRepository>(sp =>
    new LoggingChatRepository(
        sp.GetRequiredService<ChatRepository>(),
        sp.GetRequiredService<IActionLogger>()));
builder.Services.AddScoped<ChatService>();

builder.Services.AddSignalR();

// ── Coarse per-IP rate limit ─────────────────────────────────────────────────
// 120 req/min/IP across every endpoint. Rejects with 429 once the window is
// full — no queueing, no DB lookup, runs before any auth/MVC/GraphQL code.
// First line of defense for floods from a single classmate's laptop; the
// per-user MonitoredUser blocklist still handles authenticated abuse.
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ctx.Connection.RemoteIpAddress?.ToString() ?? "anon",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 120,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
            }));
});

builder.Services.AddScoped<FakerService>();
builder.Services.AddSingleton<GeneratorService>();


builder.Services
    .AddGraphQLServer()
    .AddAuthorization()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>();


string[] devOrigins =
{
    "http://localhost:5173",
    "https://localhost:5173",
    "http://127.0.0.1:5173",
    "https://127.0.0.1:5173",
};

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReact", policy =>
        policy.WithOrigins(devOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    // HSTS on localhost is a permanent foot-gun — only enable outside Dev.
    app.UseHsts();
}

app.UseStaticFiles();
app.UseHttpsRedirection();
app.UseCors("AllowReact");
app.UseRateLimiter();
app.UseAuthentication();
app.UseLoginBlock();
app.UseAuthorization();
app.MapGraphQL();
app.MapControllers();
app.MapHub<DessertHub>("/hubs/desserts");
app.MapHub<ChatHub>("/hubs/chat");
app.MapHealthChecks("/health");

await DatabaseInitializer.MigrateAndSeedAsync(app.Services);

app.Run();
