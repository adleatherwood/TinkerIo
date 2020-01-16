namespace TinkerIo

open System
open System.IO

type DbSuccess = {
    Key      : string
    Hash     : string
    Content  : string
    }

type DbFailure = {
    Key      : string
    Error    : string
    }

type DbResponse =
    | Success of DbSuccess
    | Failure of DbFailure

module private FileDbHelpers =

    let toFilename db key =
        if String.IsNullOrWhiteSpace(key)
        then None
        else Some <| Path.Combine(Config.Root, db, key)

    let toSuccess key content=
        Success {Key = key; Hash = Hash.create content; Content = content}

    let toFailure key error =
        Failure {Key = key; Error = error}

    let (|IsNew|_|) =
        Option.filter (File.Exists >> not)

    let (|IsExisting|_|) =
        Option.filter File.Exists

    let (|IsCurrent|) (h1: string, h2: string) =
        h1 = h2

    let writeAll filename key content = async {
        let! result = FileIo.write filename content
        return
            match result with
            | Ok _    -> toSuccess key content
            | Error e -> toFailure key e
    }

    let readAll filename key = async {
        let! result  = FileIo.read filename
        return
            match result with
            | Ok content -> toSuccess key content
            | Error e    -> toFailure key e
    }

    let delete filename key =
        let result = FileIo.delete filename
        match result with
        | Ok _    -> toSuccess key ""
        | Error e -> toFailure key e

module FileDb =

    open FileDbHelpers

    let create (db: string, key: string, content: string) : Async<DbResponse> = async {
        match toFilename db key with
        | IsNew filename -> return! writeAll filename key content
        | _ -> return toFailure key "File already exists"
    }

    let read (db: string, key: string) : Async<DbResponse> = async {
        match toFilename db key with
        | IsExisting filename -> return! readAll filename key
        | _ -> return toFailure key "File does not exist"
    }

    let replace (db: string, key: string, content: string) : Async<DbResponse> = async {
        match toFilename db key with
        | IsExisting filename -> return! writeAll filename key content
        | _ -> return toFailure key "File does not exist"
    }

    let update (db: string, key: string, content: string, hash: string) : Async<DbResponse> = async {
        match! read(db, key) with
        | Failure msg -> return Failure msg
        | Success red ->
            match red.Hash, hash with
            | IsCurrent true -> return! replace (db, key, content)
            | _ -> return toFailure key "Hash is out of date"
    }

    let delete (db: string, key: string) : Async<DbResponse> = async {
        return
            match toFilename db key with
            | IsExisting filename -> delete filename key
            | _ -> toSuccess key ""
    }