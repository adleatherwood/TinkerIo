namespace TinkerIo

open System
open System.IO

type Db = string
type Key = string
type Content = string
type HashCode = string

type DbRequest =
    | Create  of (Db * Key * Content)
    | Read    of (Db * Key)
    | Replace of (Db * Key * Content)
    | Update  of (Db * Key * Content * HashCode)
    | Delete  of (Db * Key)

module private DbHelpers =

    type Message = DbRequest * Control.AsyncReplyChannel<DbResponse>

    let makeWriter id =
        let writer = MailboxProcessor<Message>.Start(fun inbox ->
            let rec messageLoop() = async{
                let! request, channel = inbox.Receive()
                let! response =
                    match request with
                    | Create  c -> FileDb.create  c
                    | Read    r -> FileDb.read    r
                    | Replace r -> FileDb.replace r
                    | Update  u -> FileDb.update  u
                    | Delete  d -> FileDb.delete  d

                channel.Reply response

                return! messageLoop()
            }
            messageLoop())
        (id, writer)

    let dbOf (request: DbRequest) =
        match request with
        | Create  (db, _, _)    -> db
        | Read    (db, _)       -> db
        | Replace (db, _, _)    -> db
        | Update  (db, _, _, _) -> db
        | Delete  (db, _)       -> db

    let keyOf (request: DbRequest) =
        match request with
        | Create  (_, key, _)    -> key
        | Read    (_, key)       -> key
        | Replace (_, key, _)    -> key
        | Update  (_, key, _, _) -> key
        | Delete  (_, key)       -> key

    let indexOf maxWriters db key =
        let hash  = (db + key).GetHashCode()
        let index = hash % maxWriters |> Math.Abs
        index

module Db =

    open DbHelpers

    let private writers =
        seq {
            for id in [0 .. Config.Writers - 1] do
                yield makeWriter id
        } |> dict

    let post (request: DbRequest) : Async<DbResponse> = async {
        let db       = dbOf request
        let key      = keyOf request
        let index    = indexOf Config.Writers db key
        let response = writers.[index].PostAndAsyncReply (fun channel -> request, channel)

        return! response
    }

