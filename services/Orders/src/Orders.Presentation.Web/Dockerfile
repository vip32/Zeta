FROM mcr.microsoft.com/dotnet/aspnet:5.0-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:5.0-buster-slim AS build
WORKDIR /src
COPY ["services/Orders/src/Orders.Presentation.Web/Orders.Presentation.Web.csproj", "services/Orders/src/Orders.Presentation.Web/"]
RUN dotnet restore "services/Orders/src/Orders.Presentation.Web/Orders.Presentation.Web.csproj" -verbosity:normal
COPY . .
WORKDIR "/src/services/Orders/src/Orders.Presentation.Web"
RUN dotnet build "Orders.Presentation.Web.csproj" -c Release -o /app/build -verbosity:normal

FROM build AS publish
RUN dotnet publish "Orders.Presentation.Web.csproj" -c Release -o /app/publish -verbosity:normal

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Zeta.Orders.Presentation.Web.dll"]