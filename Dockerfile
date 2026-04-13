ARG DOTNET_VERSION=8.0

FROM mcr.microsoft.com/dotnet/sdk:$DOTNET_VERSION

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

