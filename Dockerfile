# Use the official .NET SDK image for building the app
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copy the project files to the container
COPY . ./

RUN dotnet tool install --global dotnet-ef

# Restore the dependencies (via dotnet restore)
RUN dotnet restore

# Build and publish the app
RUN dotnet publish -c Release -o out

# Use a smaller runtime image (no SDK, just the runtime)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Install EF tools globally in the runtime container
RUN apt-get update && apt-get install -y wget && wget https://packages.microsoft.com/config/debian/12/packages-microsoft-prod.deb -O packages-microsoft-prod.deb 
RUN dpkg -i packages-microsoft-prod.deb && rm packages-microsoft-prod.deb 
RUN apt-get update && apt-get install -y dotnet-sdk-9.0 && dotnet tool install --global dotnet-ef

# Copy the published output from the build container
COPY --from=build /app/out .

# Expose port 5000 (or your desired port)
EXPOSE 5000

# Set the command to run the app (after applying migrations)
CMD ["dotnet", "Clipser.dll"]
