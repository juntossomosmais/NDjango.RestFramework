ROM juntossomosmais/dotnet-sonar:6.0

# Install EF Core tools
RUN dotnet tool install --global dotnet-ef --version 6.0.2

WORKDIR /app
COPY . ./