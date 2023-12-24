using System.Text;
using Application.Options;
using Application.Services;
using Infrastructure;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace API.Configurations;

public static class DependencyInjection
{
    public static IServiceCollection ConfigureCors(this IServiceCollection services)
    {
        services.AddCors(config =>
        {
            config.AddPolicy("CorsPolicy", policy =>
            {
                policy
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowAnyOrigin();
            });
        });
        return services;
    }
    public static IServiceCollection ConfigureSwagger(this IServiceCollection services)
    {
        services
            .AddEndpointsApiExplorer()
            .AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "TestApiJWT", Version = "v1" });
            });

        return services;
    }
    public static IServiceCollection ConfigureControllers(this IServiceCollection services)
    {
        services
            .AddControllers();

        return services;
    }

    public static IServiceCollection ConfigureMail(this IServiceCollection services, ConfigurationManager configuration)
    {
        services.Configure<MailSettings>(configuration.GetSection("MailSettings"));
        return services;
    }

    public static IServiceCollection ConfigureAuthentication(this IServiceCollection services, ConfigurationManager configuration)
    {
        services.AddScoped<IAuthService, AuthService>();

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddGoogle(options =>
            {
                options.ClientId = configuration["GoogleAuthentication:Id"]!;
                options.ClientSecret = configuration["GoogleAuthentication:Secret"]!;
                
            })
            .AddJwtBearer(config =>
            {
                config.TokenValidationParameters = new()
                {
                    ValidIssuer = configuration["JwtSettings:Issuer"],
                    ValidAudience = configuration["JwtSettings:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JwtSettings:Key"]!)),
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                };
            });
            
        return services;
    }
    public static IServiceCollection ConfigureAuthorization(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder();
        return services;
    }
    public static IServiceCollection ConfigureIdentity(this IServiceCollection services)
    {
        services
            .AddIdentity<AppUser, IdentityRole>()
            .AddEntityFrameworkStores<AppDbContext>();

        return services;
    }
    public static IServiceCollection ConfigureDbContext(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("constr");
        services.AddDbContext<AppDbContext>(config =>
        {   
            config.UseSqlServer(connectionString, b => b.MigrationsAssembly(nameof(Infrastructure)));
        });
        return services;
    }
    public static IServiceCollection ConfigureOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection("JwtSettings"));
        services.Configure<GoogleAuthenticationOptions>(configuration.GetSection("GoogleAuthentication"));

        return services;
    }
    public static IServiceCollection ConfigureAutoMapper(this IServiceCollection services)
    {
        services.AddAutoMapper(config => config.AddProfile<MappingProfile>());
        return services;
    }

}
