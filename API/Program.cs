using API.Configurations;
using Infrastructure.Data;
using Infrastructure.Hubs;

var builder = WebApplication.CreateBuilder(args);


builder.Services
	.ConfigureControllers()
	.ConfigureCors()
	.ConfigureAuthentication(builder.Configuration)
	.ConfigureRepositores()
	.ConfigureDbContext(builder.Configuration, builder.Environment)
	.ConfigureHttpClients(builder.Configuration, builder.Environment.IsDevelopment())
	.ConfigureOptions(builder.Configuration)
	.ConfigureServices()
	.ConfigureAutoMapper()
	.ConfigureSwagger();


var app = builder.Build();

app.UseDeveloperExceptionPage();
app.UseSwagger();
app.UseSwaggerUI(c => c
	.SwaggerEndpoint("/swagger/v1/swagger.json", "TestApiJWT v1"));

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseCors("AllowAll");

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.MapHub<NotificationHub>("notification-hub");

app.InitializeDatabase();

app.Run();