using API.Configurations;
using dotenv.net;
using Infrastructure.Data;
using Infrastructure.Hubs;
using Microsoft.AspNetCore.HttpOverrides;

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
    // HSTS is deliberately not enabled: it would advertise
    // Strict-Transport-Security over plain HTTP and browsers would
    // refuse to connect at all. Add it only behind a real certificate.
}

app.UseSwagger();
app.UseSwaggerUI(c =>
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "TestApiJWT v1"));

// HTTPS redirection is opt-in via RequireHttps=true. Somee's free plan serves
// the site over plain HTTP on a *.somee.com domain and does not issue a
// certificate for free subdomains (only paid plans renew automatically), so an
// unconditional UseHttpsRedirection would bounce every request into a loop
// and the site would never answer.
if (string.Equals(builder.Configuration["RequireHttps"], "true",
                   StringComparison.OrdinalIgnoreCase))
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();

// IIS puts the app behind a reverse proxy, so honour the forwarded scheme.
// This must be enabled for the opt-in HTTPS redirect to see the real scheme.
app.UseForwardedHeaders();

app.UseCors("Default");

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.MapHub<NotificationHub>("notification-hub");

app.MapGet("/error", () => Results.Problem(
    title: "An error occurred",
    statusCode: 500));

// Liveness/readiness endpoint. The deploy pipeline and the host's own
// monitoring both need a URL that answers without a token, without a
// database round-trip and without touching an external service - anything
// else fails open (a passing probe that was really a broken dependency)
// or fails closed (a real outage reported as healthy). This is the only
// unauthenticated route that reports on the host rather than the app's own
// plumbing, which is what makes it safe to expose.
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    service = "mental-mediator-api",
    time = DateTime.UtcNow
}));

// await app.InitializeDatabaseAsync();

app.Run();
