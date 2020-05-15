FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["services/ApiGateway/src/ApiGateway.Application.Web/ApiGateway.Application.Web.csproj", "services/ApiGateway/src/ApiGateway.Application.Web/"]
RUN dotnet restore "services/ApiGateway/src/ApiGateway.Application.Web/ApiGateway.Application.Web.csproj" -verbosity:normal
COPY . .
WORKDIR "/src/services/ApiGateway/src/ApiGateway.Application.Web"
RUN dotnet build "ApiGateway.Application.Web.csproj" -c Release -o /app/build -verbosity:normal

FROM build AS publish
RUN dotnet publish "ApiGateway.Application.Web.csproj" -c Release -o /app/publish -verbosity:normal

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Zeta.ApiGateway.Application.Web.dll"]