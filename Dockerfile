ARG DOTNET_VERSION=8.0

FROM mcr.microsoft.com/dotnet/sdk:$DOTNET_VERSION

WORKDIR /app

# https://github.com/dotnet/dotnet-docker/issues/520
ENV PATH="${PATH}:/root/.dotnet/tools"

# Tools used during development
RUN dotnet tool install --global dotnet-ef
RUN dotnet tool install --global dotnet-format

# Restores (downloads) all NuGet packages from all projects of the solution on a separate layer
COPY src/AspNetCore.RestFramework.Core/*.csproj ./src/AspNetCore.RestFramework.Core/
COPY tests/AspNetCore.RestFramework.Core.Test/*.csproj ./tests/AspNetCore.RestFramework.Core.Test/

RUN dotnet restore src/AspNetCore.RestFramework.Core
RUN dotnet restore tests/AspNetCore.RestFramework.Core.Test

COPY . ./

