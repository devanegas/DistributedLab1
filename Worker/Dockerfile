FROM mcr.microsoft.com/dotnet/core/runtime:2.2-stretch-slim AS base
WORKDIR /app



FROM mcr.microsoft.com/dotnet/core/sdk:2.2-stretch AS build
WORKDIR /src
COPY ["Worker/Worker.csproj", "Worker/"]
RUN dotnet restore "Worker/Worker.csproj"
COPY ["Shared/Shared.csproj", "Shared/"]
COPY ["Shared/*.cs", "Shared/"]
RUN dotnet restore "Shared/Shared.csproj"
COPY . .
WORKDIR "/src/Worker"
RUN dotnet build "Worker.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "Worker.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "Worker.dll"]
