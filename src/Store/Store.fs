namespace TinkerIo.Store

open System
open TinkerIo
open TinkerIo.Crud
open System.IO

type Db = string
type Key = string
type Hash = string
type Content = string
type HashCode = string

module Io =

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


module private StoreHelpers =

    type Message = CrudRequest * Control.AsyncReplyChannel<CrudResult>

    let makeWriter id =
        let writer = MailboxProcessor<Message>.Start(fun inbox ->
            let rec messageLoop() = async{
                let! request, channel = inbox.Receive()
                let! response = Crud.post Io.Services request

                channel.Reply response

                return! messageLoop()
            }
            messageLoop())
        (id, writer)

    let dbOf (request: CrudRequest) =
        match request with
        | Create  (db, _, _)    -> db
        | Read    (db, _)       -> db
        | Replace (db, _, _)    -> db
        | Update  (db, _, _, _) -> db
        | Delete  (db, _)       -> db
        | Publish (db, _, _)    -> db

    let keyOf (request: CrudRequest) =
        match request with
        | Create  (_, key, _)    -> key
        | Read    (_, key)       -> key
        | Replace (_, key, _)    -> key
        | Update  (_, key, _, _) -> key
        | Delete  (_, key)       -> key
        | Publish (_, key, _)    -> key

    let indexOf maxWriters db key =
        let hash  = (db + key).GetHashCode()
        let index = hash % maxWriters |> Math.Abs
        index

module Store =

    open StoreHelpers

    let private writers =
        seq {
            for id in [0 .. Config.StoreWriters - 1] do
                yield makeWriter id
        } |> dict

    let post (request: CrudRequest) = async {
        let db       = dbOf request
        let key      = keyOf request
        let index    = indexOf Config.StoreWriters db key
        let response = writers.[index].PostAndAsyncReply (fun channel -> request, channel)

        return! response
    }

