using API.Configurations;
using dotenv.net;
using Infrastructure.Data;
using Infrastructure.Hubs;

DotEnv.Load(options: new DotEnvOptions(probeForEnv: true));
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
    .ConfigureRepositories()
    .ConfigureAutoMapper()
    .ConfigureServices()
    .ConfigureCaching()
    .ConfigureHttpClients(builder.Configuration, builder.Environment.IsDevelopment());

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseSwagger();
app.UseSwaggerUI(c =>
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "TestApiJWT v1"));

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseForwardedHeaders();

app.UseCors("Default");

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.MapHub<NotificationHub>("notification-hub");

app.MapGet("/error", () => Results.Problem(
/*  */    title: "An error occurred",
    statusCode: 500));

// await app.InitializeDatabaseAsync();

app.Run();

