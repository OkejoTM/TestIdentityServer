using System.Reflection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Добавляем сервисы
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opt =>
{
    opt.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter Jwt token:"
    });
    opt.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
    
});

// Настройка аутентификации с Duende IdentityServer
builder.Services.AddAuthentication()
    .AddJwtBearer(options =>
    {
        options.Authority = "http://localhost:5231"; // Изменено на 5231
        options.TokenValidationParameters.ValidateAudience = false;
        options.RequireHttpsMetadata = false; // Только для разработки

    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("EmployeeOnly", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("scope", "checkMate");
        policy.RequireRole("Employee");
    });
    
    // Политика для офис-менеджеров
    options.AddPolicy("OfficeManagerOnly", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("scope", "checkMate");
        policy.RequireRole("OfficeManager");
    });
});

var app = builder.Build();

// Настройка middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication(); // Важно добавить перед UseAuthorization
app.UseAuthorization();

app.MapGet("/secure", [Authorize] (HttpContext context) => 
{
    var user = context.User;
    return new
    {
        username = user.Identity?.Name,
        claims = user.Claims.Select(c => new { c.Type, c.Value })
    };
});

// Endpoint только для сотрудников
app.MapGet("/employee", [Authorize(Policy = "EmployeeOnly")] () => 
    "This endpoint is for employees only!");

// Endpoint только для офис-менеджеров
app.MapGet("/manager", [Authorize(Policy = "OfficeManagerOnly")] () => 
    "This endpoint is for office managers only!");

app.MapGet("identity", (ClaimsPrincipal user) => user.Claims.Select(c => new { c.Type, c.Value }))
    .RequireAuthorization("EmployeeOnly");

app.Run();

