FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["services/Orders/src/Orders.Application.Web/Orders.Application.Web.csproj", "services/Orders/src/Orders.Application.Web/"]
RUN dotnet restore "services/Orders/src/Orders.Application.Web/Orders.Application.Web.csproj" -verbosity:normal
COPY . .
WORKDIR "/src/services/Orders/src/Orders.Application.Web"
RUN dotnet build "Orders.Application.Web.csproj" -c Release -o /app/build -verbosity:normal

FROM build AS publish
RUN dotnet publish "Orders.Application.Web.csproj" -c Release -o /app/publish -verbosity:normal

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Zeta.Orders.Application.Web.dll"]