FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/TestJob.Api/TestJob.Api.csproj src/TestJob.Api/
RUN dotnet restore src/TestJob.Api/TestJob.Api.csproj
COPY src/TestJob.Api/ src/TestJob.Api/
RUN dotnet publish src/TestJob.Api/TestJob.Api.csproj -c Release --no-restore -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
ENV ASPNETCORE_URLS=http://+:8090
EXPOSE 8090
ENTRYPOINT ["dotnet", "TestJob.Api.dll"]
