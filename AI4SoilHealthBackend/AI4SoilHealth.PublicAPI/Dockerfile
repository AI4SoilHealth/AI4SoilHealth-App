FROM mcr.microsoft.com/dotnet/sdk:latest as build
WORKDIR /app
COPY *.csproj ./
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o out
FROM mcr.microsoft.com/dotnet/aspnet:latest
WORKDIR /app
COPY --from=build /app/out .
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://*:5012
EXPOSE 5012
ENTRYPOINT ["dotnet", "AI4SoilHealth.PublicAPI.dll"]