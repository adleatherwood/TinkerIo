namespace TinkerIo

open System
open System.IO

module FileIo =

    let location db key =
        if String.IsNullOrWhiteSpace(db) || String.IsNullOrWhiteSpace(key)
        then None
        else Some <| Path.Combine(Config.StoreRoot, db, key)

    let exists filename =
        File.Exists filename

    let read filename = async {
        try
            let! content = File.ReadAllTextAsync(filename) |> Async.AwaitTask
            return Ok content
        with
        | e -> return Error e.Message
    }

    let rec write filename content = async {
        try
            File.WriteAllTextAsync(filename, content) |> Async.AwaitTask |> ignore
            return Ok ()
        with
        | :? DirectoryNotFoundException ->
            FileInfo(filename).DirectoryName |> Directory.CreateDirectory |> ignore
            return! write filename content
        | e -> return Error e.Message
    }

    let delete filename =
        try
            Ok <| File.Delete(filename)
        with
        | e -> Error e.Message

    let Services : CrudIo = {
        Location = location
        Exists = exists
        Write = write
        Read = read
        Delete = delete
    }
