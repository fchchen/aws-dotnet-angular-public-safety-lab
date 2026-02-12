FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY PublicSafetyLab.sln ./
COPY src/PublicSafetyLab.Contracts/PublicSafetyLab.Contracts.csproj src/PublicSafetyLab.Contracts/
COPY src/PublicSafetyLab.Domain/PublicSafetyLab.Domain.csproj src/PublicSafetyLab.Domain/
COPY src/PublicSafetyLab.Application/PublicSafetyLab.Application.csproj src/PublicSafetyLab.Application/
COPY src/PublicSafetyLab.Infrastructure/PublicSafetyLab.Infrastructure.csproj src/PublicSafetyLab.Infrastructure/
COPY src/PublicSafetyLab.Worker/PublicSafetyLab.Worker.csproj src/PublicSafetyLab.Worker/
RUN dotnet restore src/PublicSafetyLab.Worker/PublicSafetyLab.Worker.csproj
COPY . .
RUN dotnet publish src/PublicSafetyLab.Worker/PublicSafetyLab.Worker.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./
ENTRYPOINT ["dotnet", "PublicSafetyLab.Worker.dll"]
