using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TestAdAuth.Config;
using TestAdAuth.Data;
using TestAdAuth.Models;
using TestAdAuth.Services;


var builder = WebApplication.CreateBuilder(args);

// Добавление сервисов
builder.Services.Configure<DatabaseConfig>(builder.Configuration.GetSection(DatabaseConfig.Section));

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Services.BuildServiceProvider().GetRequiredService<IOptions<DatabaseConfig>>().Value.ConnectionString));

// Настройка Identity
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Настройка LDAP
builder.Services.Configure<LdapSettings>(
    builder.Configuration.GetSection("LdapSettings"));

// Регистрация сервисов
builder.Services.AddScoped<ILdapService, LdapService>();
builder.Services.AddScoped<IUserSyncService, UserSyncService>();

// Настройка IdentityServer
builder.Services
    .AddIdentityServer(options =>
    {
        options.IssuerUri = "http://localhost:5231";
    })
    .AddInMemoryIdentityResources(IdentityConfiguration.IdentityResources)
    .AddInMemoryApiScopes(IdentityConfiguration.ApiScopes)
    .AddInMemoryApiResources(IdentityConfiguration.ApiResources)
    .AddInMemoryClients(IdentityConfiguration.Clients)
    .AddAspNetIdentity<ApplicationUser>()
    .AddResourceOwnerValidator<ResourceOwnerPasswordValidator>()
    .AddProfileService<ProfileService>()
    .AddDeveloperSigningCredential();

builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

var app = builder.Build();

// Применение миграций при запуске
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseIdentityServer();
app.UseAuthorization();

app.MapGet("/", () => "IdentityServer is running!");

app.Run();