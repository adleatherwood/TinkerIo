namespace TinkerIo.Cache

open TinkerIo
open TinkerIo.Crud
open System.Collections.Concurrent

module Io =
    // todo no db level?
    let private documents = ConcurrentDictionary<string,string>()

    let location (db: string) (key: string) =
        Some (db + key)

    let exists key =
        documents.ContainsKey(key)

    let read key = async {
        match documents.TryGetValue key with
        | true, value -> return Ok value
        | _ -> return Error "Document does not exists"
    }

    let rec write key content = async {
        documents.AddOrUpdate(key, content, (fun _ _ -> content)) |> ignore
        return Ok ()
    }

    let delete key =
        documents.TryRemove key |> ignore
        Ok ()

    let Services : CrudIo = {
        Location = location
        Exists = exists
        Write = write
        Read = read
        Delete = delete
    }

module Cache =

    let post = Crud.post Io.Services
