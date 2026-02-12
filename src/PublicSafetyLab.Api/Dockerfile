FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY PublicSafetyLab.sln ./
COPY src/PublicSafetyLab.Contracts/PublicSafetyLab.Contracts.csproj src/PublicSafetyLab.Contracts/
COPY src/PublicSafetyLab.Domain/PublicSafetyLab.Domain.csproj src/PublicSafetyLab.Domain/
COPY src/PublicSafetyLab.Application/PublicSafetyLab.Application.csproj src/PublicSafetyLab.Application/
COPY src/PublicSafetyLab.Infrastructure/PublicSafetyLab.Infrastructure.csproj src/PublicSafetyLab.Infrastructure/
COPY src/PublicSafetyLab.Api/PublicSafetyLab.Api.csproj src/PublicSafetyLab.Api/
RUN dotnet restore src/PublicSafetyLab.Api/PublicSafetyLab.Api.csproj
COPY . .
RUN dotnet publish src/PublicSafetyLab.Api/PublicSafetyLab.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "PublicSafetyLab.Api.dll"]
