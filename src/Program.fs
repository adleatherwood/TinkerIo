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
                    .UseUrls(sprintf "http://*:%i" Config.Port) |> ignore)

    [<EntryPoint>]
    let main args =
        printfn ""
        printfn "CONFIGURATION"
        printfn "------------------------------------------------------------------------"
        printfn "   STORE   : %s" Config.StoreRoot
        printfn "   STREAM  : %s" Config.StreamRoot
        printfn "   WRITERS : %i" Config.StoreWriters
        printfn "   PORT    : %i" Config.Port
        printfn "------------------------------------------------------------------------"

        CreateHostBuilder(args).Build().Run()
        exitCode
