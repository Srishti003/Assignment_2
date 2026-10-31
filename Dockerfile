# Stage 1: Build the application using .NET 10 SDK
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Copy solution and project definitions to optimize Docker layer caching
COPY BankingLedger.slnx ./
COPY src/BankingLedger.Domain/BankingLedger.Domain.csproj src/BankingLedger.Domain/
COPY src/BankingLedger.Application/BankingLedger.Application.csproj src/BankingLedger.Application/
COPY src/BankingLedger.Infrastructure/BankingLedger.Infrastructure.csproj src/BankingLedger.Infrastructure/
COPY src/BankingLedger.Api/BankingLedger.Api.csproj src/BankingLedger.Api/
COPY tests/BankingLedger.UnitTests/BankingLedger.UnitTests.csproj tests/BankingLedger.UnitTests/

# Restore NuGet dependencies
RUN dotnet restore BankingLedger.slnx

# Copy the remaining source code
COPY . .

# Build and publish the API in Release mode
WORKDIR /app/src/BankingLedger.Api
RUN dotnet publish -c Release -o /out

# Stage 2: Runtime image using lightweight ASP.NET Core 10 runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Set ASP.NET Core environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Development

# Expose container HTTP port
EXPOSE 8080

# Copy compiled artifacts from build stage
COPY --from=build /out .

# Run the API
ENTRYPOINT ["dotnet", "BankingLedger.Api.dll"]
