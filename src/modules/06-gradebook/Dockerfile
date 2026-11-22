FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base

WORKDIR /app

# Create writable directory for the SQLite database.
RUN mkdir -p /app/data && chown $APP_UID:$APP_UID /app/data

USER $APP_UID
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Gradebook/GradebookAPI/GradebookAPI.csproj", "Gradebook/GradebookAPI/"]
RUN dotnet restore "Gradebook/GradebookAPI/GradebookAPI.csproj"
COPY . .
WORKDIR "/src/Gradebook/GradebookAPI"
RUN dotnet build "./GradebookAPI.csproj" -c $BUILD_CONFIGURATION -o /app/build --no-restore

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./GradebookAPI.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false --no-restore

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "GradebookAPI.dll"]