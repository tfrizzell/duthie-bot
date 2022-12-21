FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env
WORKDIR /build

# Copy csproj and restore as distinct layers
COPY . ./
RUN dotnet restore && \
    dotnet publish -c Release -o dist

# Build runtime image
FROM mcr.microsoft.com/dotnet/runtime:6.0
WORKDIR /app
COPY --from=build-env /build/dist .
ENTRYPOINT ["./duthie.exe"]