FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY MobileCatalogAPI.csproj .
RUN dotnet restore MobileCatalogAPI.csproj
COPY . .
RUN dotnet publish MobileCatalogAPI.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MobileCatalogAPI.dll"]
