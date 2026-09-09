using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MyTasks.Contexts;
using MyTasks.Exceptions;
using MyTasks.Middleware;
using MyTasks.Repositories;
using MyTasks.Services;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);



// Add services to the container.

builder.Services.AddProblemDetails(configure =>
{
    configure.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions.TryAdd("requestId", context.HttpContext.TraceIdentifier);
    };
});
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters
            .Add(new JsonStringEnumConverter());
    });
// Add DbContext
builder.Services.AddDbContext<MyTasks.Data.MyTasksContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("MyTasksContext") ?? "Data Source=MyTasks.db"));

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Register repositories and services
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITaskRepository, TaskRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IGuestSessionRepository, GuestSessionRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<ITokenCleanupService, TokenCleanupService>();
builder.Services.AddScoped<IGuestSessionCleanupService, GuestSessionCleanupService>();

if(!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddSingleton<IHostedService>(sp => new PeriodicCleanupBackgroundService<ITokenCleanupService>(
    sp.GetRequiredService<IServiceScopeFactory>(),
    sp.GetRequiredService<ILogger<PeriodicCleanupBackgroundService<ITokenCleanupService>>>(),
    TimeSpan.FromHours(1),
    "old refresh tokens"));

    builder.Services.AddSingleton<IHostedService>(sp => new PeriodicCleanupBackgroundService<IGuestSessionCleanupService>(
        sp.GetRequiredService<IServiceScopeFactory>(),
        sp.GetRequiredService<ILogger<PeriodicCleanupBackgroundService<IGuestSessionCleanupService>>>(),
        TimeSpan.FromHours(1),
        "expired guest sessions"));
}

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IGuestTokenService, GuestTokenService>();
builder.Services.AddScoped<IGuestSessionService, GuestSessionService>();
builder.Services.AddScoped<ITaskOwnerResolver, TaskOwnerResolver>();

builder.Services.AddScoped<ITaskOwnerContext, TaskOwnerContext>();

// JWT authentication
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"]
    ?? throw new ConfigurationException("Jwt:Key is not configured. Set it in appsettings.json or user secrets.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSection["Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    await MyTasks.Data.AdminSeeder.SeedAsync(scope.ServiceProvider, app.Configuration);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandler();

if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();

app.UseMiddleware<TaskOwnerMiddleware>();

app.UseAuthorization();

app.MapControllers();

app.Run();

// Makes Program accessible to WebApplicationFactory<Program> -
// top-level statements generate this class as internal by default.
public partial class Program { }