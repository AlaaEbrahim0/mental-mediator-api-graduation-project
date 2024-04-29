# Use the appropriate base image
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Set the working directory
WORKDIR /src

# Copy project files

COPY [Shared.csproj, Shared/] 
COPY [Domain.csproj, Domain/]
COPY [Application.csproj, Application/] 
COPY [Infrastructure.csproj, Infrastructure/]
COPY [API.csproj, API/]

RUN dotnet restore "./API/API.csproj"

# Copy the entire solution and build
COPY . .
WORKDIR "/src/API"
RUN dotnet build "./API.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "./API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final stage for production
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

# Set the working directory
WORKDIR /app

# Copy published files
COPY --from=publish /app/publish .

# Set the user
USER app

# Expose ports
EXPOSE 8080
EXPOSE 8081

# Set the entry point
ENTRYPOINT ["dotnet", "API.dll"]
