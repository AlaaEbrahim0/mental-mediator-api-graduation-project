using System.Globalization;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Application.Contracts;
using Application.Dtos.SchedulingDtos;
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
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace API.Configurations;

public static class DependencyInjection
{
	private static readonly string[] VerifiedProductionOrigins =
	{
		"https://mental-health-ochre-xi.vercel.app",
		"https://mental-mediator-doctor-ui-graduation-project.vercel.app",
		"https://mental-mediator-doctor-ui-graduation-project-lms6panp1.vercel.app",
	};

	private static readonly string[] LocalDevelopmentOrigins =
	{
		"https://localhost:7221",
		"http://localhost:5184",
		"http://localhost:5068",
		"http://localhost:5500",
		"http://localhost:3000",
		"http://localhost:3001",
		"http://localhost:5173",
	};

	public static IServiceCollection ConfigureCors(this IServiceCollection services, IConfiguration configuration)
	{
		// The allowlist is deploy-time configuration: an operator can replace it
		// with CORS_ALLOWED_ORIGINS (comma-separated) or Cors:AllowedOrigins
		// without rebuilding the API. Only origins verified as belonging to this
		// project are compiled in; stale third-party deployments are not.
		var configuredOrigins =
			configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
			?? Environment.GetEnvironmentVariable("CORS_ALLOWED_ORIGINS")
				?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
			?? Array.Empty<string>();

		var origins = configuredOrigins.Length > 0
			? configuredOrigins
			: VerifiedProductionOrigins.Concat(LocalDevelopmentOrigins).ToArray();

		services.AddCors(config =>
		{
			config.AddPolicy("Default", policy =>
			{
				policy.WithOrigins(origins)
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
				config.JsonSerializerOptions.Converters.Add(new CloudinaryUrlJsonConverter());

			});

		services.AddValidatorsFromAssemblyContaining<UpdateDoctorScheduleRequest>();
		services.AddFluentValidationAutoValidation();
		services.AddFluentValidationClientsideAdapters();

		return services;
	}
	public static IServiceCollection ConfigureRepositories(this IServiceCollection services)
	{
		services.AddScoped<IRepositoryManager, RepositoryManager>();
		services.AddScoped<IPostRepository, PostRepository>();
		services.AddScoped<IPostMediaAssetRepository, PostMediaAssetRepository>();
		services.AddScoped<ICommentRepository, CommentRepository>();
		services.AddScoped<IReplyRepository, ReplyRepository>();
		services.AddScoped<INotificationRepository, NotificationRepository>();
		services.AddScoped<IChatRoomRepository, ChatRoomRepository>();
		services.AddScoped<IChatMessageRepository, ChatMessageRepository>();
		services.AddScoped<IChatMessageReportRepository, ChatMessageReportRepository>();
		services.AddScoped<IDoctorRepository, DoctorRepository>();
		services.AddScoped<IUserRepository, UserRepository>();
		services.AddScoped<IProfileRepository, ProfileRepository>();
		services.AddScoped<IAppointmentRepository, AppointmentRepository>();
		services.AddScoped<IDoctorSchedulingRepository, DoctorSchedulingRepository>();
		services.AddScoped<IDepressionTestResultRepository, DepressionTestResultRepository>();
		return services;
	}

	public static IServiceCollection ConfigureHttpClients(this IServiceCollection services, ConfigurationManager configuration)
	{
		services.AddScoped<IHateSpeechDetector, HateSpeechDetectorClient>();
		services.AddScoped<IDepressionDetector, DepressionDetectorClient>();
		services.AddScoped<INewsService, NewsApiClient>();

		services.AddHttpClient<NewsApiClient>("news-api-client", config =>
		{
			var newsApiConfig = configuration.GetSection("NewsApi");
			config.BaseAddress = new Uri(newsApiConfig["Address"]!);
			config.DefaultRequestHeaders.Add("X-Api-Key", newsApiConfig["ApiKey"]!);
			config.DefaultRequestHeaders.UserAgent.ParseAdd("Nexus");
		});

		var mlServerAddress = configuration["MLServerAddress"];
		if (string.IsNullOrWhiteSpace(mlServerAddress))
		{
			throw new InvalidOperationException("MLServerAddress must be configured for content moderation.");
		}

		// One client for the hosted model. The budget in MLServiceProbe decides
		// how long a whole attempt sequence may take; this bounds a single
		// hung request so no caller can wait longer than the cold-start window.
		services.AddHttpClient("ml-client", config =>
		{
			config.BaseAddress = new Uri(mlServerAddress);
			config.Timeout = TimeSpan.FromSeconds(Math.Max(1, configuration.GetValue("MLServer:TimeoutSeconds", 45)));
		});

		services.AddSingleton<MLServiceProbe>();
		// No polling keep-alive is registered for the model service (the
		// dormant Infrastructure/BackgroundJobs/HostingRefresher.cs): a request
		// that arrives asleep is served by waiting inside the probe budget,
		// not by a timer that spends resources whether or not anyone is typing.
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
			.AddScoped<IDoctorSchedulingService, DoctorSchedulingService>()
			.AddScoped<ClaimsPrincipal>()
			.AddScoped<MachineLearningService>()
			.AddTransient<MailTemplates>()
			.AddScoped<IUserService, UserService>()
			.AddScoped<IDoctorService, DoctorService>()
			.AddScoped<IProfileService, ProfileService>()
			.AddScoped<IStorageService, CloudinaryStorageService>()
			// Room passphrases are a separate credential class from account
			// passwords: a room secret must never be able to authenticate a
			// user, so it gets its own hasher rather than IPasswordHasher.
			.AddSingleton<IRoomPassphraseHasher, RoomPassphraseHasher>()
			.AddSingleton<IRoomJoinThrottle, RoomJoinThrottle>()
			.AddScoped<IWebRootFileProvider, WebRootFileProvider>()
			// Resend is the preferred transactional email provider. MailService
			// (MailKit SMTP) is the fallback ResendMailService delegates to when
			// Resend is unconfigured. It is wired by hand rather than by the
			// generic overload because ResendMailService itself implements
			// IMailService: letting the container inject that parameter resolves
			// IMailService -> ResendMailService -> IMailService and every consumer
			// of IMailService (booking, auth) fails at activation.
			.AddTransient<MailService>()
			.AddScoped<IMailService>(services => new ResendMailService(
				services.GetRequiredService<IResendClient>(),
				services.GetRequiredService<IOptions<ResendSettings>>(),
				services.GetRequiredService<MailService>(),
				services.GetRequiredService<ILogger<ResendMailService>>()))
			.AddScoped<IDepressionTestService, DepressionTestService>()
			.AddScoped<IAdminService, AdminService>()
			.AddScoped<IChatService, ChatService>()
			.AddScoped<IDoctorWorkspaceService, DoctorWorkspaceService>()
			.AddScoped<IPatientDirectoryLookup, PatientDirectoryLookup>()
			.AddSingleton<IChatConnectionRegistry, ChatConnectionRegistry>()
			.AddSingleton<IChatRealtimeNotifier, ChatRealtimeNotifier>()
			.AddSignalR()
			.AddJsonProtocol(options =>
			{
				// Keep hub payload enums consistent with the REST API, which
				// already serializes enums as strings.
				options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter());
				options.PayloadSerializerOptions.Converters.Add(new CloudinaryUrlJsonConverter());
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

			// Rooms used to be doctor-only. They are not, so the staff budget is
			// kept and patients get a much smaller one: the moderation queue is
			// human, and two rooms an hour is plenty for a consultation request
			// or a follow-up room without letting one account fill the listing.
			// The partition is keyed on the role claim, so a token minted before a
			// role change is budgeted under the old role until it is refreshed.
			options.AddPolicy(RateLimitPolicies.ChatRoomCreation, context =>
			{
				var isPatient = GetRoomCreationTier(context) == PatientCreationTier;

				return RateLimitPartition.GetFixedWindowLimiter(
					$"chat-room-creation:{GetRateLimitPartitionKey(context)}",
					_ => new FixedWindowRateLimiterOptions
					{
						PermitLimit = isPatient ? 2 : 5,
						Window = TimeSpan.FromHours(1),
						QueueLimit = 0,
						AutoReplenishment = true
					});
			});

			options.AddPolicy(RateLimitPolicies.ChatRoomJoin, context =>
				RateLimitPartition.GetFixedWindowLimiter(
					GetRateLimitPartitionKey(context),
					_ => new FixedWindowRateLimiterOptions
					{
						PermitLimit = 10,
						Window = TimeSpan.FromMinutes(5),
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

			// Authentication endpoints: per-IP limit to blunt brute-force and
			// account-enumeration probing. 10 per minute is generous for a human
			// retrying credentials or requesting a reset link, but blocks rapid
			// automation. Uses a IP-only partition key (auth flows may run before
			// a user is identified).
			options.AddPolicy(RateLimitPolicies.Auth, context =>
				RateLimitPartition.GetFixedWindowLimiter(
					context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
					_ => new FixedWindowRateLimiterOptions
					{
						PermitLimit = 10,
						Window = TimeSpan.FromMinutes(1),
						QueueLimit = 0,
						AutoReplenishment = true
					}));

			// Google sign-in is three browser navigations per login and the
			// rejected response is unreadable to a user mid-redirect, so this
			// window is deliberately wider than the credential one. It still
			// caps fast automation from a single address.
			options.AddPolicy(RateLimitPolicies.OAuthHandoff, context =>
				RateLimitPartition.GetFixedWindowLimiter(
					context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
					_ => new FixedWindowRateLimiterOptions
					{
						PermitLimit = 60,
						Window = TimeSpan.FromMinutes(1),
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

	private const string PatientCreationTier = "patient";

	/// <summary>
	/// Splits the room-creation budget so opening rooms to patients does not give
	/// one account an unlimited supply of them. Anyone who is not staff is held to
	/// the patient budget, including an unauthenticated partition, because that is
	/// the conservative reading of the claim set.
	/// </summary>
	private static string GetRoomCreationTier(HttpContext context) =>
		context.User.IsInRole("Doctor") || context.User.IsInRole("Admin")
			? "staff"
			: PatientCreationTier;

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

					// A rejected, expired or missing state/correlation cookie fails
					// inside the OAuth handler, before any controller runs, and the
					// default response is a 500 rendered as raw JSON in a top-level
					// browser navigation. Send the same allow-listed redirect shape the
					// callback uses so the portal can show readable copy.
					options.Events = new OAuthEvents
					{
						OnRemoteFailure = context =>
						{
							context.HandleResponse();

							context.HttpContext.RequestServices
								.GetRequiredService<ILoggerFactory>()
								.CreateLogger("GoogleOAuth")
								.LogWarning(
									"Google handshake failed before the callback: {Error}",
									context.Failure?.Message);

							var portal = AuthHandoff.ResolveBaseUrl(
								configuration,
								context.HttpContext.Request,
								AuthHandoff.PatientSetting);

							context.Response.Redirect(AuthHandoff.BuildFailureRedirect(
								portal,
								AuthHandoff.GenericFailureCode));

							return Task.CompletedTask;
						}
					};
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

		var resendSection = configuration.GetSection("Resend");
		// Allow RESEND_API_KEY (the canonical env var name) to override the
		// appsettings "Resend:ApiKey" value. If both are empty, Resend is
		// simply disabled and MailKit SMTP is used as the fallback.
		var envApiKey = Environment.GetEnvironmentVariable("RESEND_API_KEY");
		if (!string.IsNullOrWhiteSpace(envApiKey))
		{
			configuration[$"Resend:ApiKey"] = envApiKey;
		}
		services.Configure<ResendSettings>(resendSection);

		return services;
	}

	public static IServiceCollection ConfigureResend(this IServiceCollection services)
	{
		// Register the Resend HTTP client with a named client so the API key
		// (set up in the ResendClient constructor) is never serialized into a
		// generic HttpClient. The base address and timeout are configured in
		// the client itself, but RegisterHttpClient here keeps the DI chain
		// explicit and testable.
		services.AddHttpClient<IResendClient, ResendClient>();
		return services;
	}

	public static IServiceCollection ConfigureAutoMapper(this IServiceCollection services)
	{
		services.AddAutoMapper(config => config.AddProfile<MappingProfile>());
		return services;
	}

}
