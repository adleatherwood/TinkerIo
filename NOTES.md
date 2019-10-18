

```ps
docker run -d --name gitlab-runner --restart always -v D:/Code/bin/gitlab:/etc/gitlab-runner -v /var/run/docker.sock:/var/run/docker.sock gitlab/gitlab-runner:latest
```

```ps
docker exec -it <container id> bash
```

```ps

cd /src
dotnet publish -c Release -r linux-arm --self-contained
scp -rp bin/Release/netcoreapp3.0/linux-arm/publish pi@192.168.1.90:tinkerio/publish
password
cd ..
scp -p dockerfile  pi@192.168.1.90:.
password
ssh pi@192.168.1.90
password

```

-p:Version=1.2.3.4

./TinkerIo --urls http://*:5000

https://www.atlascode.com/blog/running-asp-net-core-in-an-alpine-linux-docker-container/

ASPNETCORE_URLS="http://0.0.0.0:5000