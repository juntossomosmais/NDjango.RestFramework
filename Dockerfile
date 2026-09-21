ARG DOTNET_VERSION=10.0

FROM mcr.microsoft.com/dotnet/sdk:$DOTNET_VERSION

# The package multi-targets net8.0/net9.0/net10.0 and the suite runs once per framework, so every
# targeted runtime has to be here — the SDK image ships only its own (10.0). The aspnet images are
# the ones to copy from: they carry Microsoft.AspNetCore.App as well as Microsoft.NETCore.App, and
# the tests spin up a WebApplicationFactory.
COPY --from=mcr.microsoft.com/dotnet/aspnet:8.0 /usr/share/dotnet/shared /usr/share/dotnet/shared
COPY --from=mcr.microsoft.com/dotnet/aspnet:9.0 /usr/share/dotnet/shared /usr/share/dotnet/shared

WORKDIR /app

# Restores (downloads) all NuGet packages from all projects of the solution on a separate layer.
# Directory.Build.props has to come along: it is where the per-target-framework package versions
# are resolved, so without it the PackageReference versions evaluate to empty. The lock files have
# to come along for the same reason --locked-mode exists.
COPY *.sln Directory.Build.props ./
COPY src/NDjango.RestFramework/*.csproj src/NDjango.RestFramework/packages.lock.json ./src/NDjango.RestFramework/
COPY tests/NDjango.RestFramework.Test/*.csproj tests/NDjango.RestFramework.Test/packages.lock.json ./tests/NDjango.RestFramework.Test/
RUN dotnet restore --locked-mode

# Tools used during development
COPY dotnet-tools.json ./
RUN dotnet tool restore

COPY . ./

