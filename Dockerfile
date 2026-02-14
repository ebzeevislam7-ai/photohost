### Multi-stage Dockerfile for ASP.NET Core PhotoHost
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# copy csproj and restore as distinct layers
COPY ./PhotoHost.csproj ./
RUN dotnet restore ./PhotoHost.csproj

# copy everything else and publish
COPY . .
RUN dotnet publish ./PhotoHost.csproj -c Release -o /app/publish -r linux-x64 --self-contained false /p:PublishTrimmed=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./

ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80

ENTRYPOINT ["dotnet", "PhotoHost.dll"]
