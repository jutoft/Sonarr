## Build from project root using "docker build . -f docker/builder/Dockerfile" or "podman build . -f docker/builder/Dockerfile"
FROM docker.io/node:18 as nodebuilder

RUN corepack enable

COPY package.json yarn.lock .yarnrc /app/
WORKDIR /app

RUN yarn install

COPY tsconfig.json /app/
COPY frontend/ /app/frontend

RUN yarn run build --env production

FROM mcr.microsoft.com/dotnet/sdk:6.0 as dotnetbuilder
#FROM docker.io/alpine:3 as dotnetbuilder

#RUN apk add dotnet6-sdk --no-cache

COPY .editorconfig /app/
COPY Logo/ /app/Logo
COPY src/ /app/src
WORKDIR /app
ARG PLATFORM=Posix
ARG CONFIGURATION=Release
ARG RID_LIMIT=-p:RuntimeIdentifiers=linux-musl-x64

# -p:Platform=${PLATFORM} -p:RuntimeIdentifiers=${RID} # -t:PublishAllRids
#RUN dotnet test /app/Sonarr.sln
#RUN dotnet build /app/src/Sonarr.sln --configuration Release
#RUN dotnet msbuild -restore /app/src/Sonarr.sln -p:Configuration=Release -p:Platform=Posix

#RUN dotnet clean src/Sonarr.sln -c Debug
#RUN dotnet msbuild -restore /app/src/Sonarr.sln -p:Configuration=Release -p:Platform=Posix
#RUN dotnet msbuild -restore src/Sonarr.sln -p:Configuration=Debug -p:Platform=Posix -t:PublishAllRids ; ls -la /app/_output
RUN dotnet msbuild -restore src/Sonarr.sln -p:Configuration=${CONFIGURATION} -p:Platform=${PLATFORM} ${RID_LIMIT} -t:PublishAllRids
#RUN dotnet build /app/src/Sonarr.sln /property:GenerateFullPaths=true /consoleloggerparameters:NoSummary

#RUN ls -la /app/_output/net6.0/linux-x64

#FROM mcr.microsoft.com/dotnet/runtime:6.0 as dotnetruntime
#FROM docker.io/alpine:latest as dotnetruntime
FROM ghcr.io/linuxserver/baseimage-alpine:3.18
RUN apk add --no-cache \
    icu-libs \
    sqlite-libs \
    xmlstarlet
COPY --from=dotnetbuilder /app/_output/net6.0/linux-musl-x64 /app/sonarr/bin
COPY --from=nodebuilder /app/_output/UI /app/sonarr/bin/UI
# Symlink UI folder to parent dir to allow running in both Debug and Release mode
RUN ln -s /app/sonarr/bin/UI /app/sonarr

VOLUME /config
CMD [ "/app/sonarr/bin/Sonarr", "-nobrowser", "-data=/config" ]
