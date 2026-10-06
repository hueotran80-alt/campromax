# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore
COPY ["WebBanCameraGiamSat.csproj", "./"]
RUN dotnet restore "WebBanCameraGiamSat.csproj"

# Copy everything else and build
COPY . .
RUN dotnet publish "WebBanCameraGiamSat.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "WebBanCameraGiamSat.dll"]
