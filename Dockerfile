# ==============================================================================
# Build Stage: Restore and Compile
# ==============================================================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project definition and restore NuGet dependencies to leverage layer caching
COPY ["src/EmployeeApi/EmployeeApi.csproj", "src/EmployeeApi/"]
RUN dotnet restore "src/EmployeeApi/EmployeeApi.csproj"

# Copy all source files and compile Release output
COPY . .
WORKDIR "/src/src/EmployeeApi"
RUN dotnet publish "EmployeeApi.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

# ==============================================================================
# Final Runtime Stage: Hardened, Non-root .NET 8 Container
# ==============================================================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Switch to the pre-configured non-root 'app' user (UID 1654) for Kubernetes security
USER $APP_UID

# Port 8080 is the default non-root listening port in .NET 8
EXPOSE 8080

# Configure container environment variables
ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_EnableDiagnostics=0

# Copy compiled artifacts from build stage
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "EmployeeApi.dll"]

