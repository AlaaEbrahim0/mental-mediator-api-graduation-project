using API.Configurations;
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

var app = builder.Build();

app.UseDeveloperExceptionPage();
app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "TestApiJWT v1"));

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseCors("CorsPolicy");

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.InitializeDatabase(builder.Environment.IsProduction());

app.Run();