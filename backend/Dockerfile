# build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Backend.Api.csproj ./
RUN dotnet restore Backend.Api.csproj

COPY . ./
RUN dotnet publish Backend.Api.csproj -c Release -o /app/publish

# run
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8000
ENTRYPOINT ["dotnet", "Backend.Api.dll"]