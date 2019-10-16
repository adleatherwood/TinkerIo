

```ps
docker run -d --name gitlab-runner --restart always -v D:/Code/bin/gitlab:/etc/gitlab-runner -v /var/run/docker.sock:/var/run/docker.sock gitlab/gitlab-runner:latest
```

```ps
docker exec -it <container id> bash
```

```ps

cd /src
dotnet publish -c Release -r linux-arm --self-contained
bin/Release/netcoreapp3.0/linux-arm/publish/
```

-p:Version=1.2.3.4