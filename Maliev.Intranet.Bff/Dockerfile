FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /
COPY nuget.config ./
COPY ["Maliev.Intranet.Bff/Maliev.Intranet.Bff.csproj", "Maliev.Intranet.Bff/"]
COPY ["Maliev.Intranet.Client/Maliev.Intranet.Client.csproj", "Maliev.Intranet.Client/"]
COPY ["Maliev.Intranet.Shared/Maliev.Intranet.Shared.csproj", "Maliev.Intranet.Shared/"]

RUN --mount=type=secret,id=nuget_username \
    --mount=type=secret,id=nuget_password \
    NUGET_USERNAME=$(cat /run/secrets/nuget_username) \
    NUGET_PASSWORD=$(cat /run/secrets/nuget_password) \
    dotnet restore "Maliev.Intranet.Bff/Maliev.Intranet.Bff.csproj"

COPY . .
WORKDIR "/Maliev.Intranet.Bff"
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
RUN chown -R app:app /app
USER app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
HEALTHCHECK CMD curl -f http://localhost:8080/intranet/liveness || exit 1
ENTRYPOINT ["dotnet", "Maliev.Intranet.Bff.dll"]

