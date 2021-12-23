ARG DOTNETCORE_VERSION=5.0

# Starting layer point using Microsoft's dotnet SDK image based on debian distro
FROM mcr.microsoft.com/dotnet/sdk:$DOTNETCORE_VERSION

ARG JAVA_JRE_VERSION=11

# Java for Sonar:
# 1. adds debian's buster release repository with bin pkgs (deb) to a new 'docker.list' repo sources file
# 2. updates pkgs and installs openjdk-$JAVA_JRE_VERSION-jre package with java from debian's repository
RUN echo "deb http://ftp.de.debian.org/debian buster main" | tee /etc/apt/sources.list.d/docker.list && \
    apt-get update && apt-get install -y openjdk-$JAVA_JRE_VERSION-jre

# Dotnet sonarscanner global tool installation
RUN dotnet tool install --global dotnet-sonarscanner
ENV PATH="${PATH}:/root/.dotnet/tools"

RUN dotnet tool install --global dotnet-ef

WORKDIR /app
COPY . ./
