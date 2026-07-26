# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0.302 AS build
WORKDIR /src
COPY src/TrainingManagement.Domain/TrainingManagement.Domain.csproj src/TrainingManagement.Domain/
COPY src/TrainingManagement.Application/TrainingManagement.Application.csproj src/TrainingManagement.Application/
COPY src/TrainingManagement.Infrastructure/TrainingManagement.Infrastructure.csproj src/TrainingManagement.Infrastructure/
COPY src/TrainingManagement.Web/TrainingManagement.Web.csproj src/TrainingManagement.Web/
COPY src/TrainingManagement.Migrations/TrainingManagement.Migrations.csproj src/TrainingManagement.Migrations/
RUN dotnet restore src/TrainingManagement.Web/TrainingManagement.Web.csproj \
    && dotnet restore src/TrainingManagement.Migrations/TrainingManagement.Migrations.csproj
COPY . .
RUN dotnet publish src/TrainingManagement.Web/TrainingManagement.Web.csproj -c Release --no-restore -o /out/web /p:UseAppHost=false
RUN dotnet publish src/TrainingManagement.Migrations/TrainingManagement.Migrations.csproj -c Release --no-restore -o /out/migrations /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0.5 AS final
WORKDIR /app
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://0.0.0.0:8080 \
    PORT=8080 \
    DOTNET_EnableDiagnostics=0
COPY --from=build /out/web ./
COPY --from=build /out/migrations ./migrations/
USER root
RUN mkdir -p /app/data/keys /app/data/certificates /app/data/temp \
    && chown -R app:app /app/data
USER app
EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=5s --start-period=15s --retries=3 \
  CMD ["dotnet", "TrainingManagement.Web.dll", "--healthcheck"]
ENTRYPOINT ["dotnet", "TrainingManagement.Web.dll"]
