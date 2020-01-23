namespace TinkerIo

open System
open System.IO

module private Cli =

    let find name =
        Environment.GetCommandLineArgs()
        |> Array.filter (StringCi.startsWith name)
        |> Array.map (String.splitSnd '=')
        |> Array.tryHead

module private ConfigHelpers =

    let defaultData = Path.Combine(Directory.GetCurrentDirectory() , "data")

    let envData =
        Environment.GetEnvironmentVariable("TINKERIO_DATA")
        |> Option.ofObj

    let cmdData =
        Cli.find "--data"

    let defaultStore = "stores"

    let envStore =
        Environment.GetEnvironmentVariable("TINKERIO_STORE")
        |> Option.ofObj

    let cmdStore =
        Cli.find "--store"

    let defaultWriters = 1000

    let envWriters =
        Environment.GetEnvironmentVariable("TINKERIO_WRITERS")
        |> Option.ofObj
        |> Option.map Int32.Parse

    let cmdWriters =
        Cli.find "--writers"
        |> Option.map Int32.Parse

    let defaultStream = "streams"

    let envStream =
        Environment.GetEnvironmentVariable("TINKERIO_STREAM")
        |> Option.ofObj

    let cmdStream =
        Cli.find "--stream"

    let defaultPort = 5000

    let envPort =
        Environment.GetEnvironmentVariable("TINKERIO_PORT")
        |> Option.ofObj
        |> Option.map Int32.Parse

    let cmdPort =
        Cli.find "--port"
        |> Option.map Int32.Parse

module Config =

    open ConfigHelpers

    let private dataRoot =
        cmdData
        |> Option.orElse envData
        |> Option.defaultValue defaultData

    let StoreRoot =
        cmdStore
        |> Option.orElse envStore
        |> Option.defaultValue (Path.Combine(dataRoot, defaultStore))

    let StoreWriters =
        cmdWriters
        |> Option.orElse envWriters
        |> Option.defaultValue defaultWriters

    let StreamRoot =
        cmdStream
        |> Option.orElse envStream
        |> Option.defaultValue (Path.Combine(dataRoot, defaultStream))

    let Port =
        cmdPort
        |> Option.orElse envPort
        |> Option.defaultValue defaultPort
