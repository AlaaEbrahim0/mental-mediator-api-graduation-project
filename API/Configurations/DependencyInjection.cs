﻿using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using Application.Contracts;
using Application.Dtos.WeeklyScheduleDtos;
using Application.Options;
using Application.Services;
using Application.Utilities;
using Domain.Entities;
using Domain.Repositories;
using FluentValidation;
using FluentValidation.AspNetCore;
using Infrastructure.Caching;
using Infrastructure.Clients;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Infrastructure.Utilities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
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
			config.AddPolicy("Default", policy =>
			{
				policy
					.WithOrigins(
						"https://mental-health-ochre.vercel.app",
						"https://mental-mediator-doctor-ui-graduation-project.vercel.app",
						"https://mental-health-b2uehhu7l-rana-tareks-projects.vercel.app",
						"https://nexus-admin-panel.vercel.app",
						 "https://mental-health-drab.vercel.app",
						"https://localhost:7221",
						"http://localhost:5068",
						"http://localhost:5500",
						"http://localhost:3000")
						.AllowAnyHeader()
						.AllowAnyMethod()
						.AllowCredentials()
						.WithExposedHeaders();

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

		services.AddControllers()
			.AddJsonOptions(config =>
			{
				config.JsonSerializerOptions
				.Converters.Add(new JsonStringEnumConverter());

			});

		services.AddValidatorsFromAssemblyContaining<CreateScheduleWeekDayRequest>();
		services.AddFluentValidationAutoValidation();
		services.AddFluentValidationClientsideAdapters();

		return services;
	}
	public static IServiceCollection ConfigureRepositories(this IServiceCollection services)
	{
		services.AddScoped<IRepositoryManager, RepositoryManager>();
		services.AddScoped<IPostRepository, PostRepository>();
		services.AddScoped<ICommentRepository, CommentRepository>();
		services.AddScoped<IReplyRepository, ReplyRepository>();
		services.AddScoped<INotificationRepository, NotificationRepository>();
		services.AddScoped<IDoctorRepository, DoctorRepository>();
		services.AddScoped<IUserRepository, UserRepository>();
		services.AddScoped<IAppointmentRepository, AppointmentRepository>();
		services.AddScoped<IDoctorScheduleRepository, DoctorScheduleRepository>();
		services.AddScoped<IDepressionTestResultRepository, DepressionTestResultRepository>();
		return services;
	}

	public static IServiceCollection ConfigureHttpClients(this IServiceCollection services, ConfigurationManager configuration, bool envIsDev)
	{
		services.AddScoped<IHateSpeechDetector, HateSpeechDetectorClient>();
		services.AddScoped<IDepressionDetector, DepressionDetectorClient>();
		services.AddScoped<INewsService, NewsApiClient>();

		//if (!envIsDev)
		//{
		//	services.AddHostedService<HostingRefresher>();
		//}

		//services.AddHttpClient<HostingRefresher>("self", config =>
		//{
		//	var baseAddress = configuration["BaseAddress"];
		//	config.BaseAddress = new Uri(baseAddress!);
		//});

		services.AddHttpClient<NewsApiClient>("news-api-client", config =>
		{
			var newsApiConfig = configuration.GetSection("NewsApi");
			config.BaseAddress = new Uri(newsApiConfig["Address"]!);
			config.DefaultRequestHeaders.Add("X-Api-Key", newsApiConfig["ApiKey"]!);
			config.DefaultRequestHeaders.UserAgent.ParseAdd("Nexus");
		});

		services.AddHttpClient<DepressionDetectorClient>("ml-client", config =>
		{
			var baseAddress = configuration["MLServerAddress"];
			config.BaseAddress = new Uri(baseAddress!);
		});

		services.AddHttpClient<HateSpeechDetectorClient>("ml-client", config =>
		{
			var baseAddress = configuration["MLServerAddress"];
			config.BaseAddress = new Uri(baseAddress!);
		});


		return services;
	}

	public static IServiceCollection ConfigureServices(this IServiceCollection services)
	{
		services
			.AddScoped<IPostService, PostService>()
			.AddScoped<ICommentService, CommentService>()
			.AddScoped<IReplyService, ReplyService>()
			.AddScoped<IUserClaimsService, UserClaimsService>()
			.AddScoped<INotificationService, NotificationService>()
			.AddScoped<INotificationSender, NotificationSender>()
			.AddScoped<IAppointmentService, AppointmentService>()
			.AddScoped<IWeeklyScheduleService, WeeklyScheduleService>()
			.AddScoped<ClaimsPrincipal>()
			.AddScoped<MachineLearningService>()
			.AddTransient<MailTemplates>()
			.AddScoped<IUserService, UserService>()
			.AddScoped<IDoctorService, DoctorService>()
			.AddScoped<IStorageService, CloudinaryStorageService>()
			.AddScoped<IWebRootFileProvider, WebRootFileProvider>()
			.AddTransient<IMailService, MailService>()
			.AddScoped<IDepressionTestService, DepressionTestService>()
			.AddScoped<IAdminService, AdminService>()
			.AddSignalR();

		services.Configure<ForwardedHeadersOptions>(options =>
		{
			options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
		});

		return services;
	}
	public static IServiceCollection ConfigureCaching(this IServiceCollection services)
	{
		services
			.AddSingleton<ICacheService, InMemoryCacheService>()
			.AddMemoryCache();

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
				options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
			})
			.AddJwtBearer(config =>
			{
				config.TokenValidationParameters = new()
				{
					ValidIssuer = configuration["JwtSettings:Issuer"],
					ValidAudience = configuration["JwtSettings:Audience"],
					IssuerSigningKey =
						new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JwtSettings:Key"]!)),
					ValidateIssuer = true,
					ValidateAudience = true,
					ValidateIssuerSigningKey = true,
					ValidateLifetime = true,
				};
			})
			.AddGoogle(options =>
			{
				options.ClientId = configuration["GoogleAuthentication:Id"]!;
				options.ClientSecret = configuration["GoogleAuthentication:Secret"]!;
			});



		return services;
	}

	public static IServiceCollection ConfigureIdentity(this IServiceCollection services)
	{
		services.AddIdentity<BaseUser, IdentityRole>(options =>
		{
		})
			.AddEntityFrameworkStores<AppDbContext>()
			.AddDefaultTokenProviders();

		return services;

	}

	public static IServiceCollection ConfigureAuthorization(this IServiceCollection services)
	{
		services.AddAuthorizationBuilder();
		return services;
	}

	public static IServiceCollection ConfigureDbContext(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment env)
	{
		var connectionString = string.Empty;
		if (env.IsDevelopment())
		{
			connectionString = configuration.GetConnectionString("constr");
		}
		else
		{
			connectionString = configuration.GetConnectionString("constr_somee");
		}

		services.AddDbContext<AppDbContext>(config =>
		{
			config.UseSqlServer(connectionString, b => b.MigrationsAssembly(nameof(Infrastructure)));
			config.EnableDetailedErrors();
			config.EnableSensitiveDataLogging();
			config.LogTo(Console.WriteLine);

		});

		return services;
	}

	public static IServiceCollection ConfigureOptions(this IServiceCollection services, IConfiguration configuration)
	{
		services.Configure<JwtOptions>(configuration.GetSection("JwtSettings"));
		services.Configure<GoogleAuthenticationOptions>(configuration.GetSection("GoogleAuthentication"));
		services.Configure<MailSettings>(configuration.GetSection("MailSettings"));

		return services;
	}

	public static IServiceCollection ConfigureAutoMapper(this IServiceCollection services)
	{
		services.AddAutoMapper(config => config.AddProfile<MappingProfile>());
		return services;
	}

}
