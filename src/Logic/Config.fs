namespace TinkerIo

open System

module private Cli =

    let find name =
        Environment.GetCommandLineArgs()
        |> Array.filter (String.startsWithCi name)
        |> Array.map (String.splitSnd '=')
        |> Array.tryHead

module private ConfigHelpers =

    let defaultRoot = "./db"

    let envRoot =
        Environment.GetEnvironmentVariable("TINKERIO_ROOT")
        |> Option.ofObj

    let cmdRoot =
        Cli.find "--root"

    let defaultWriters = 1000

    let envWriters =
        Environment.GetEnvironmentVariable("TINKERIO_WRITERS")
        |> Option.ofObj
        |> Option.map Int32.Parse

    let cmdWriters =
        Cli.find "--root"
        |> Option.map Int32.Parse

module Config =

    open ConfigHelpers

    let Writers =
        cmdWriters
        |> Option.orElse envWriters
        |> Option.defaultValue defaultWriters

    let Root =
        cmdRoot
        |> Option.orElse envRoot
        |> Option.defaultValue defaultRoot
