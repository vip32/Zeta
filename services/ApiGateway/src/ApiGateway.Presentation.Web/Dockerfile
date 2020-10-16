FROM mcr.microsoft.com/dotnet/aspnet:5.0-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:5.0-buster-slim AS build
WORKDIR /src
COPY ["services/ApiGateway/src/ApiGateway.Presentation.Web/ApiGateway.Presentation.Web.csproj", "services/ApiGateway/src/ApiGateway.Presentation.Web/"]
RUN dotnet restore "services/ApiGateway/src/ApiGateway.Presentation.Web/ApiGateway.Presentation.Web.csproj" -verbosity:normal
COPY . .
WORKDIR "/src/services/ApiGateway/src/ApiGateway.Presentation.Web"
RUN dotnet build "ApiGateway.Presentation.Web.csproj" -c Release -o /app/build -verbosity:normal

FROM build AS publish
RUN dotnet publish "ApiGateway.Presentation.Web.csproj" -c Release -o /app/publish -verbosity:normal

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Zeta.ApiGateway.Presentation.Web.dll"]