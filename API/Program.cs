using API.Configurations;
using Infrastructure.Data;
using Infrastructure.Hubs;

var builder = WebApplication.CreateBuilder(args);


builder.Services
	.ConfigureControllers()
	.ConfigureSwagger()
	.ConfigureCors()
	.ConfigureOptions(builder.Configuration)
	.ConfigureDbContext(builder.Configuration, builder.Environment)
	.ConfigureIdentity()
	.ConfigureAuthentication(builder.Configuration)
	.ConfigureAuthorization()
	.ConfigureRepositores()
	.ConfigureAutoMapper()
	.ConfigureServices()
	.ConfigureCaching()
	.ConfigureHttpClients(builder.Configuration, builder.Environment.IsDevelopment());


var app = builder.Build();

app.UseDeveloperExceptionPage();

app.UseSwagger();
app.UseSwaggerUI(c => c
	.SwaggerEndpoint("/swagger/v1/swagger.json", "TestApiJWT v1"));

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseCors("Default");

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.MapHub<NotificationHub>("notification-hub");

app.InitializeDatabase();

app.Run();