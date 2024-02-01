using System.Security.Claims;
using System.Text;
using Application.Options;
using Application.Services;
using Domain.Entities;
using Infrastructure.Contracts;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Infrastructure.Utilities;
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
            .AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo { Title = "Mental Mediator", Version = "v1" });
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                });
                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            },
                            Name = "Bearer",
                            In = ParameterLocation.Header,

                        },
                        new List<string>()
                    }
                });
            });

        return services;
    }
    public static IServiceCollection ConfigureControllers(this IServiceCollection services)
    {
        services
            .AddControllers();

        return services;
    }

    public static IServiceCollection ConfigureRepositores(this IServiceCollection services)
    {
        services.AddScoped<IRepositoryManager, RepositoryManager>();
        services.AddScoped<IPostRepository, PostRepository>();
        services.AddScoped<ICommentRepository, CommentRepository>();
        services.AddScoped<IReplyRepository, ReplyRepository>();
        return services;
    }

    public static IServiceCollection ConfigureMailSettings(this IServiceCollection services, ConfigurationManager configuration)
    {
        services.Configure<MailSettings>(configuration.GetSection("MailSettings"));
        return services;
    }

    public static IServiceCollection ConfigureMailService(this IServiceCollection services)
    {
        services.AddTransient<IMailService, MailService>();
        return services;
    }

    public static IServiceCollection ConfigureEntityServices(this IServiceCollection services)
    {
        services.AddScoped<IPostService, PostService>();
        services.AddScoped<ICommentService, CommentService>();
        services.AddScoped<IReplyService, ReplyService>();
        services.AddScoped<ClaimsPrincipal>();
        return services;
    }

    public static IServiceCollection ConfigureAuthentication(this IServiceCollection services, ConfigurationManager configuration)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddTransient<JwtTokenGenerator>();

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
            .AddIdentity<AppUser, IdentityRole>(options =>
            {
                options.SignIn.RequireConfirmedEmail = true;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

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
