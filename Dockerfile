FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY MobileCatalog.sln .
COPY src/MobileCatalog.Domain/MobileCatalog.Domain.csproj src/MobileCatalog.Domain/
COPY src/MobileCatalog.Application/MobileCatalog.Application.csproj src/MobileCatalog.Application/
COPY src/MobileCatalog.Infrastructure/MobileCatalog.Infrastructure.csproj src/MobileCatalog.Infrastructure/
COPY src/MobileCatalog.WebApi/MobileCatalog.WebApi.csproj src/MobileCatalog.WebApi/
RUN dotnet restore src/MobileCatalog.WebApi/MobileCatalog.WebApi.csproj
COPY . .
RUN dotnet publish src/MobileCatalog.WebApi/MobileCatalog.WebApi.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MobileCatalog.WebApi.dll"]
