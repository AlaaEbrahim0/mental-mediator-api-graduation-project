using System.Globalization;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
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
using Infrastructure.Hubs;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Infrastructure.Utilities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
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
				policy.WithOrigins(
                // Your Vercel Deployments (Production & Previews)
                "https://mental-health-ochre.vercel.app",
                "https://mental-mediator-doctor-ui-graduation-project.vercel.app",
                "https://mental-mediator-doctor-ui-graduation-project-lms6panp1.vercel.app", // <--- Added your active URL
                "https://mental-health-b2uehhu7l-rana-tareks-projects.vercel.app",
                "https://nexus-admin-panel.vercel.app",
                "https://mental-health-drab.vercel.app",
                // Local Development
                // 5173 (Vite default) was missing, so a browser on the default
                // Vite URL got a CORS failure that looked like a broken API.
                "https://localhost:7221",
                "http://localhost:5184",
                "http://localhost:5068",
                "http://localhost:5500",
                "http://localhost:3000",
                "http://localhost:5173"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();

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

		services.AddControllers(options =>
		{
			options.Filters.Add<ValidateModelStateFilter>();
		})
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
		services.AddScoped<IChatRoomRepository, ChatRoomRepository>();
		services.AddScoped<IChatMessageRepository, ChatMessageRepository>();
		services.AddScoped<IChatMessageReportRepository, ChatMessageReportRepository>();
		services.AddScoped<IDoctorRepository, DoctorRepository>();
		services.AddScoped<IUserRepository, UserRepository>();
		services.AddScoped<IAppointmentRepository, AppointmentRepository>();
		services.AddScoped<IDoctorScheduleRepository, DoctorScheduleRepository>();
		services.AddScoped<IDoctorSchedulingRepository, DoctorSchedulingRepository>();
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

		// Chat moderation is deliberately isolated from the existing ML client:
		// it gets its own short timeout and is called on the chat path only.
		services.AddHttpClient("chat-ml-client", config =>
		{
			var baseAddress = configuration["MLServerAddress"];
			if (string.IsNullOrWhiteSpace(baseAddress))
			{
				throw new InvalidOperationException("MLServerAddress must be configured for chat moderation.");
			}

			config.BaseAddress = new Uri(baseAddress);
			config.Timeout = TimeSpan.FromSeconds(3);
		});
		services.AddScoped<IChatContentModerator, ChatContentModerator>();

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
			.AddScoped<IDoctorSchedulingService, DoctorSchedulingService>()
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
			.AddScoped<IChatService, ChatService>()
			.AddSingleton<IChatConnectionRegistry, ChatConnectionRegistry>()
			.AddSingleton<IChatRealtimeNotifier, ChatRealtimeNotifier>()
			.AddSignalR()
			.AddJsonProtocol(options =>
			{
				// Keep hub payload enums consistent with the REST API, which
				// already serializes enums as strings.
				options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter());
			});

		// On shared IIS hosting the app pool has neither a user profile nor the
		// HKLM registry, so the default data-protection provider warns that it
		// is falling back to an ephemeral in-memory key repository: keys die
		// with the worker process and every restart invalidates issued cookies
		// and OAuth state payloads. Persist the keys to a folder in the app's
		// physical directory instead. The folder is outside the app's static
		// web root, so the keys are never served as files, and the deploy
		// pipeline keeps it across releases with the DoNotDeleteRule.
		// A failure here (for example an unwritable folder) downgrades to the
		// previous ephemeral behaviour instead of blocking startup.
		try
		{
			var keysPath = Path.Combine(AppContext.BaseDirectory, "keys");
			Directory.CreateDirectory(keysPath);
			services.AddDataProtection()
				.PersistKeysToFileSystem(new DirectoryInfo(keysPath));
		}
		catch (Exception)
		{
			// Fall back to the ephemeral default.
		}

		// Honour the reverse-proxy headers IIS adds in front of the app. This is
		// what lets the app see the real client address and the real scheme,
		// which the opt-in HTTPS redirect in Program.cs depends on.
		services.Configure<ForwardedHeadersOptions>(options =>
		{
			options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
		});

		return services;
	}

	public static IServiceCollection ConfigureRateLimiting(this IServiceCollection services)
	{
		services.AddRateLimiter(options =>
		{
			options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

			options.AddPolicy(RateLimitPolicies.ChatMessages, context =>
				RateLimitPartition.GetFixedWindowLimiter(
					GetRateLimitPartitionKey(context),
					_ => new FixedWindowRateLimiterOptions
					{
						PermitLimit = 30,
						Window = TimeSpan.FromMinutes(1),
						QueueLimit = 0,
						AutoReplenishment = true
					}));

			options.AddPolicy(RateLimitPolicies.ChatRoomCreation, context =>
				RateLimitPartition.GetFixedWindowLimiter(
					GetRateLimitPartitionKey(context),
					_ => new FixedWindowRateLimiterOptions
					{
						PermitLimit = 5,
						Window = TimeSpan.FromHours(1),
						QueueLimit = 0,
						AutoReplenishment = true
					}));

			options.AddPolicy(RateLimitPolicies.ChatReports, context =>
				RateLimitPartition.GetFixedWindowLimiter(
					GetRateLimitPartitionKey(context),
					_ => new FixedWindowRateLimiterOptions
					{
						PermitLimit = 10,
						Window = TimeSpan.FromHours(1),
						QueueLimit = 0,
						AutoReplenishment = true
					}));

			options.OnRejected = async (context, cancellationToken) =>
			{
				context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
				context.HttpContext.Response.ContentType = "application/problem+json";

				if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
				{
					context.HttpContext.Response.Headers.RetryAfter =
						Math.Ceiling(retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
				}

				await context.HttpContext.Response.WriteAsJsonAsync(
					new ProblemDetails
					{
						Status = StatusCodes.Status429TooManyRequests,
						Title = "Too Many Requests",
						Detail = "You have reached the rate limit for this action. Please try again later."
					},
					cancellationToken);
			};
		});

		return services;
	}

	private static string GetRateLimitPartitionKey(HttpContext context)
	{
		var userId = context.User.FindFirst("uid")?.Value
			?? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

		return string.IsNullOrWhiteSpace(userId)
			? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous"
			: userId;
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

		var authenticationBuilder = services
			.AddAuthentication(options =>
			{
				options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
				options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
				options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
			})
			.AddJwtBearer(config =>
			{
				// Keep the raw claim names from the token. The default inbound
				// mapping rewrites the "roles" claim to the long
				// http://schemas.microsoft.com/ws/2008/06/identity/claims/role URI,
				// so matching against "roles" found nothing and every
				// [Authorize(Roles = "Doctor")] endpoint answered 403.
				config.MapInboundClaims = false;

				config.TokenValidationParameters = new()
				{
					ValidIssuer = configuration["JwtSettings:Issuer"],
					ValidAudience = configuration["JwtSettings:Audience"],
					IssuerSigningKey =
						new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JwtSettings:Key"]!)),
					RoleClaimType = "roles",
					NameClaimType = "name",
					ValidateIssuer = true,
					ValidateAudience = true,
					ValidateIssuerSigningKey = true,
					ValidateLifetime = true,
				};
				config.Events = new JwtBearerEvents
				{
					OnMessageReceived = context =>
					{
						var accessToken = context.Request.Query["access_token"];
						var path = context.HttpContext.Request.Path;

						// Query-string tokens are limited to the hub paths. REST
						// endpoints must continue to use the Authorization header.
						// Production request logging stays at Warning so the token
						// is not written to logs.
						if (!string.IsNullOrEmpty(accessToken) &&
							(path.StartsWithSegments("/notification-hub") ||
							 path.StartsWithSegments("/chat-hub")))
						{
							context.Token = accessToken;
						}

						return Task.CompletedTask;
					}
				};
			});
			
			var googleClientId = configuration["GoogleAuthentication:Id"];
			if (!string.IsNullOrEmpty(googleClientId) && !string.IsNullOrEmpty(configuration["GoogleAuthentication:Secret"]))
			{
					authenticationBuilder.AddGoogle(options =>
				{
					options.ClientId = googleClientId;
					options.ClientSecret = configuration["GoogleAuthentication:Secret"]!;
				});
			}


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
			// Only enable detailed errors and sensitive data logging in development
			if (env.IsDevelopment())
			{
				config.EnableDetailedErrors();
				config.EnableSensitiveDataLogging();
				config.LogTo(Console.WriteLine);
			}
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
