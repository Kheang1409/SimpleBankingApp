# Use the official .NET SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

# Set the working directory in the container
WORKDIR /app

# Copy the project files and restore the dependencies
COPY *.csproj ./ 
RUN dotnet restore

# Copy the rest of the application and build it
COPY . ./

# Publish the application
RUN dotnet publish -c Release -o out

# Use the official .NET runtime image to run the application
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app

# Copy the published files from the build container
COPY --from=build /app/out ./ 

# Set the entry point for the application
ENTRYPOINT ["dotnet", "SimpleBankingApp.dll"]
