FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["services/Customers/src/Customers.Application.Web/Customers.Application.Web.csproj", "services/Customers/src/Customers.Application.Web/"]
RUN dotnet restore "services/Customers/src/Customers.Application.Web/Customers.Application.Web.csproj" -verbosity:normal
COPY . .
WORKDIR "/src/services/Customers/src/Customers.Application.Web"
RUN dotnet build "Customers.Application.Web.csproj" -c Release -o /app/build -verbosity:normal

FROM build AS publish
RUN dotnet publish "Customers.Application.Web.csproj" -c Release -o /app/publish -verbosity:normal

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Zeta.Customers.Application.Web.dll"]