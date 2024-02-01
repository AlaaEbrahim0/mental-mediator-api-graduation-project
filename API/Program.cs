using API.Configurations;
using Infrastructure.Data;

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
    .ConfigureRepositores()
    .ConfigureDbContext(builder.Configuration);

var app = builder.Build();

app.UseDeveloperExceptionPage();
app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "TestApiJWT v1"));

app.UseHttpsRedirection();

app.UseCors("CorsPolicy");

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.InitializeDatabase();

app.Run();