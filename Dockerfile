FROM mcr.microsoft.com/dotnet/sdk:7.0 AS builder

ADD . /src

RUN dotnet dotnet build -c Release -o /app /src/EveHypernetNotification 


FROM mcr.microsoft.com/dotnet/aspnet:7.0

COPY --from=builder /app /app

ENTRYPOINT ["dotnet", "/app/EveHypernetNotification.dll"]