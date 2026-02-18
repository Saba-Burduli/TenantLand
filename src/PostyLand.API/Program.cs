using System.Text;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using PostyLand.Application;
using PostyLand.API.Auth;
using PostyLand.API.Hangfire;
using PostyLand.API.Logging;
using PostyLand.API.Middleware;
using PostyLand.Infrastructure;
using PostyLand.Persistence;
using PostyLand.Persistence.Context;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, loggerConfiguration) =>
{
    loggerConfiguration
        .MinimumLevel.Information()
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.With(services.GetRequiredService<RequestContextEnricher>())
        .WriteTo.Console(outputTemplate:
            "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}");
});

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<TenantResolutionOptions>(builder.Configuration.GetSection(TenantResolutionOptions.SectionName));
builder.Services.AddSingleton<RequestContextEnricher>();
builder.Services.AddHttpContextAccessor();

builder.Services.AddApplication();
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks()
    .AddDbContextCheck<MainDbContext>();

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
                 ?? throw new InvalidOperationException("JWT options were not configured.");
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthorizationPolicies.PlatformAdmin, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireAssertion(context => AuthorizationClaimEvaluator.IsPlatformAdmin(context.User));
    });
});

var mainDbConnectionString = builder.Configuration.GetConnectionString("MainDb")
    ?? throw new InvalidOperationException("Connection string 'MainDb' was not found.");

builder.Services.AddHangfire(configuration =>
    configuration.UsePostgreSqlStorage(options => options.UseNpgsqlConnection(mainDbConnectionString)));
builder.Services.AddHangfireServer();

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = static (diagnosticContext, httpContext) =>
    {
        var correlationId = httpContext.Items[HttpContextItemKeys.CorrelationId]?.ToString() ?? string.Empty;
        var tenantId = httpContext.Items[HttpContextItemKeys.TenantId]?.ToString() ?? string.Empty;
        var userId = httpContext.Items[HttpContextItemKeys.UserId]?.ToString() ?? string.Empty;
        diagnosticContext.Set("CorrelationId", correlationId);
        diagnosticContext.Set("TenantId", tenantId);
        diagnosticContext.Set("UserId", userId);
    };
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseMiddleware<TenantJwtScopeMiddleware>();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = [new HangfireDashboardAuthorizationFilter()]
});
app.MapControllers();

app.Run();

public partial class Program;
