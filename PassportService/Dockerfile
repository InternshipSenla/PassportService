FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

RUN dotnet tool install --global dotnet-ef

ENV PATH="$PATH:/root/.dotnet/tools"

COPY ["PassportService/PassportService.csproj", "PassportService/"]
RUN dotnet restore "./PassportService/PassportService.csproj"
COPY . .
WORKDIR "/app/PassportService"
RUN dotnet build "./PassportService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "./PassportService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PassportService.dll"]