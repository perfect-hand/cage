# https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/docker/building-net-docker-images?view=aspnetcore-10.0
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

ARG version

LABEL "org.opencontainers.image.title"="cage-backend-editor"
LABEL "org.opencontainers.image.description"="CArd Game Engine (CAGE) Editor Backend"
LABEL "org.opencontainers.image.version"="$version"
LABEL "org.opencontainers.image.source"="https://github.com/perfect-hand/cage"

WORKDIR /app
COPY bin/Release/net10.0/publish/ ./

# https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/webapplication?view=aspnetcore-10.0#listen-on-all-interfaces-using-aspnetcore_http_ports-and-aspnetcore_https_ports
ENV ASPNETCORE_HTTP_PORTS="80"
EXPOSE 80

ENTRYPOINT ["dotnet", "Cage.Backend.Editor.dll"]
