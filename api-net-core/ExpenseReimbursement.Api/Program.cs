using System.Text.Json.Serialization;
using DotNetEnv;
using ExpenseReimbursement.Api.Common;
using ExpenseReimbursement.Api.Features.Auth;
using ExpenseReimbursement.Api.Features.Employees;
using ExpenseReimbursement.Api.Features.ExpenseRequests;
using ExpenseReimbursement.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

// Carga .env (si existe) a variables de entorno del proceso ANTES de construir la config,
// para que Admin__Username/Admin__Password lleguen a IOptions<AdminOptions> como cualquier
// otra variable de entorno estándar de ASP.NET Core.
Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

const string CorsPolicyName = "AllowFrontends";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")
        ?? "Data Source=expense-reimbursement.db"));

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

builder.Services.Configure<AdminOptions>(builder.Configuration.GetSection(AdminOptions.SectionName));
builder.Services.AddSingleton<AdminTokenStore>();
builder.Services.AddScoped<AdminAuthFilter>();

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyName, policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(CorsPolicyName);

app.MapLogin();
app.MapEmployeeEndpoints();
app.MapExpenseRequestEndpoints();

app.Run();
