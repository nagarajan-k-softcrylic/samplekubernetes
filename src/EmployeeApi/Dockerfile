# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["EmployeeApi.csproj", "./"]
RUN dotnet restore "EmployeeApi.csproj"

COPY . .
RUN dotnet publish "EmployeeApi.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

USER $APP_UID
EXPOSE 8080

ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_EnableDiagnostics=0

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "EmployeeApi.dll"]

