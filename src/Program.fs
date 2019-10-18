namespace TinkerIo

open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging

module Program =
    let exitCode = 0

    let CreateHostBuilder args =
        Host.CreateDefaultBuilder(args)
            .ConfigureLogging(fun logging -> logging.AddConsole() |> ignore)
            .ConfigureWebHostDefaults(fun webBuilder ->
                webBuilder
                    .UseStartup<Startup>()
                    .UseUrls("http://*:5000;https://*:5001")|> ignore)

    [<EntryPoint>]
    let main args =
        CreateHostBuilder(args).Build().Run()
        exitCode
