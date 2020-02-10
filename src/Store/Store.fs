namespace TinkerIo.Store

open System
open TinkerIo

type Db = string
type Key = string
type Content = string
type HashCode = string

module private StoreHelpers =

    type Message = CrudRequest * Control.AsyncReplyChannel<CrudResult>

    let makeWriter id =
        let writer = MailboxProcessor<Message>.Start(fun inbox ->
            let rec messageLoop() = async{
                let! request, channel = inbox.Receive()
                let! response =
                    match request with
                    | Create  c -> Crud.create  FileIo.Services c
                    | Read    r -> Crud.read    FileIo.Services r
                    | Replace r -> Crud.replace FileIo.Services r
                    | Update  u -> Crud.update  FileIo.Services u
                    | Delete  d -> Crud.delete  FileIo.Services d
                    | Publish p -> Crud.publish FileIo.Services p

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

