# Stage 1: Build ứng dụng bằng .NET 8 SDK
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY TidsEngine.csproj ./
COPY src/ ./src/

RUN dotnet publish TidsEngine.csproj -c Release -o /app/publish

# Stage 2: Chạy ứng dụng nhẹ trên môi trường Runtime
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app

COPY --from=build /app/publish .
COPY data/ ./data/
COPY web/ ./web/

# Cổng mặc định của Render.com là 10000
ENV PORT=10000
EXPOSE 10000

ENTRYPOINT ["dotnet", "TidsEngine.dll", "web", "10000"]
