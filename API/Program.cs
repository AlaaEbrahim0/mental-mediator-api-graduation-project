using API.Configurations;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.


builder.Services
    .ConfigureControllers()
    .ConfigureCors()
    .ConfigureSwagger()
    .ConfigureIdentity()
    .ConfigureAuthentication(builder.Configuration)
    .ConfigureAuthorization()
    .ConfigureOptions(builder.Configuration)
    .ConfigureAutoMapper()
    .ConfigureDbContext(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "TestApiJWT v1"));
}

app.UseHttpsRedirection();

app.UseCors("CorsPolicy");

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();