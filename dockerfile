FROM mcr.microsoft.com/dotnet/core/runtime-deps:3.0-alpine

RUN mkdir /app
RUN mkdir /sdk

ENV PATH="/sdk:${PATH}"

COPY published /app

CMD ["./app/TinkerIo.exe"]
