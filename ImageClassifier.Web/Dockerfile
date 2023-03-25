FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["ImageClassifier.Web/ImageClassifier.Web.csproj", "ImageClassifier.Web/"]
RUN dotnet restore "ImageClassifier.Web/ImageClassifier.Web.csproj"
COPY . .
WORKDIR "/src/ImageClassifier.Web"
RUN dotnet build "ImageClassifier.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ImageClassifier.Web.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ImageClassifier.Web.dll"]
