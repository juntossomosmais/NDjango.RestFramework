ARG DOTNET_VERSION=10.0

FROM mcr.microsoft.com/dotnet/sdk:$DOTNET_VERSION

# Project targets net8.0 or net9.0 - install their runtimes so dotnet test/run works
COPY --from=mcr.microsoft.com/dotnet/runtime:8.0 /usr/share/dotnet/shared /usr/share/dotnet/shared
COPY --from=mcr.microsoft.com/dotnet/aspnet:8.0 /usr/share/dotnet/shared /usr/share/dotnet/shared
COPY --from=mcr.microsoft.com/dotnet/runtime:9.0 /usr/share/dotnet/shared /usr/share/dotnet/shared
COPY --from=mcr.microsoft.com/dotnet/aspnet:9.0 /usr/share/dotnet/shared /usr/share/dotnet/shared

WORKDIR /app

# Restores (downloads) all NuGet packages from all projects of the solution on a separate layer
COPY *.sln ./
COPY src/NDjango.RestFramework/*.csproj ./src/NDjango.RestFramework/
COPY tests/NDjango.RestFramework.Test/*.csproj ./tests/NDjango.RestFramework.Test/
RUN dotnet restore --locked-mode

# Tools used during development
COPY dotnet-tools.json ./
RUN dotnet tool restore

COPY . ./

