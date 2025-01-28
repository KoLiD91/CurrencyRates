FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 7166

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["CurrencyRates/CurrencyRates.csproj", "CurrencyRates/"]
RUN dotnet restore "CurrencyRates/CurrencyRates.csproj"
COPY . .
WORKDIR "/src/CurrencyRates"
RUN dotnet build "CurrencyRates.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CurrencyRates.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CurrencyRates.dll"]