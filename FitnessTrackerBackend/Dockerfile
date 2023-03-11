#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
#EXPOSE 443

# Swagger
ENV ASPNETCORE_URLS http://*:44319
ENV ASPNETCORE_ENVIRONMENT=Development
EXPOSE 44319

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["FitnessTrackerBackend/FitnessTrackerBackend.csproj", "FitnessTrackerBackend/"]
RUN dotnet restore "FitnessTrackerBackend/FitnessTrackerBackend.csproj"
COPY . .
WORKDIR "/src/FitnessTrackerBackend"
RUN dotnet build "FitnessTrackerBackend.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "FitnessTrackerBackend.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FitnessTrackerBackend.dll"]