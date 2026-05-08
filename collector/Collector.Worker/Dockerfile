# build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Collector.Worker.csproj ./
RUN dotnet restore Collector.Worker.csproj

COPY . ./
RUN dotnet publish Collector.Worker.csproj -c Release -o /app/publish

# run
FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Collector.Worker.dll"]