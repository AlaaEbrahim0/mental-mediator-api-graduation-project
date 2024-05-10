using API.BackgroundJobs;
using API.Configurations;
using Application.Contracts;
using Application.Utilities;
using Infrastructure.Caching;
using Infrastructure.Clients;
using Infrastructure.Data;
using Infrastructure.Hubs;
using Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);


builder.Services
	.ConfigureControllers()
	.ConfigureCors()
	.ConfigureSwagger()
	.ConfigureIdentity()
	.ConfigureAuthentication(builder.Configuration)
	.ConfigureAuthorization()
	.ConfigureMailSettings(builder.Configuration)
	.ConfigureMailService()
	.ConfigureOptions(builder.Configuration)
	.ConfigureEntityServices()
	.AddScoped<IDoctorService, DoctorService>()
	.ConfigureAutoMapper()
	.AddScoped<MailTemplates>()
	.AddScoped<IUserService, UserService>()
	.AddScoped<IStorageService, CloudinaryStorageService>()
	.AddScoped<IWebRootFileProvider, WebRootFileProvider>()
	.AddDistributedMemoryCache()
	.ConfigureRepositores()
	.ConfigureDbContext(builder.Configuration, builder.Environment);

builder.Services.AddSignalR();

builder.Services.AddSingleton<ICacheService, InMemoryCacheService>();


builder.Services.AddHttpClient<HateSpeechDetectorClient>("ml-client", config =>
{
	var baseAddress = builder.Configuration["MLServerAddress"];
	config.BaseAddress = new Uri(baseAddress!);
});

builder.Services.AddScoped<IHateSpeechDetector, HateSpeechDetectorClient>();

if (!builder.Environment.IsDevelopment())
{
	builder.Services.AddHostedService<HostingRefresher>();

	builder.Services.AddHttpClient<HostingRefresher>("self", config =>
	{
		var baseAddress = builder.Configuration["BaseAddress"];
		config.BaseAddress = new Uri(baseAddress!);
	});

}

var app = builder.Build();

app.UseDeveloperExceptionPage();
app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "TestApiJWT v1"));

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseCors("AllowAll");

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.MapHub<NotificationHub>("notification-hub");

app.InitializeDatabase();


app.Run();