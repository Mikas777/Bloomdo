using Bloomdo.Server.Api.Authorization;
using Bloomdo.Server.Infrastructure.Data;
using Bloomdo.Server.Infrastructure.Data.Repositories;
using Bloomdo.Server.Application.Interfaces;
using Bloomdo.Server.Application.Services;
using Bloomdo.Server.Application.Settings;
using Bloomdo.Server.Infrastructure.Services;
using Bloomdo.Server.Infrastructure.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

namespace Bloomdo.Server.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddDatabaseContext(this IServiceCollection serviceCollection, string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Connection string is empty or null.", nameof(connectionString));
        }

        serviceCollection.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));
    }

    public static void AddJwtAuthentication(this IServiceCollection services, JwtSettings jwtSettings)
    {
        var key = Encoding.UTF8.GetBytes(jwtSettings.Secret);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtSettings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
                RoleClaimType = ClaimTypes.Role
            };
        });

        // Permission-based authorization
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

        services.AddAuthorization();

        services.AddSingleton(jwtSettings);
    }

    public static void RegisterServices(this IServiceCollection services)
    {
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAuthSettings>(sp => sp.GetRequiredService<JwtSettings>());
    }

    public static void RegisterRepositories(this IServiceCollection services)
    {
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IRolePermissionRepository, RolePermissionRepository>();
    }
}