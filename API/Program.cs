using API.BackgroundJobs;
using API.Configurations;
using API.Hubs;
using Application.Contracts;
using Application.Utilities;
using Infrastructure.Data;
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
	.ConfigureAutoMapper()
	.AddScoped<NotificationMessageTemplates>()
	.AddScoped<IUserService, UserService>()
	.AddScoped<IStorageService, StorageService>()
	.AddScoped<IWebRootFileProvider, WebRootFileProvider>()
	.ConfigureRepositores()
	.ConfigureDbContext(builder.Configuration, builder.Environment);

builder.Services.AddSignalR();

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