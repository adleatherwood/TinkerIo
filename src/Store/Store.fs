namespace TinkerIo.Store

open System

type Db = string
type Key = string
type Content = string
type HashCode = string

type StoreRequest =
    | Create  of (Db * Key * Content)
    | Read    of (Db * Key)
    | Replace of (Db * Key * Content)
    | Update  of (Db * Key * Content * HashCode)
    | Delete  of (Db * Key)

module private StoreHelpers =

    type Message = StoreRequest * Control.AsyncReplyChannel<StoreResult>

    let makeWriter id =
        let writer = MailboxProcessor<Message>.Start(fun inbox ->
            let rec messageLoop() = async{
                let! request, channel = inbox.Receive()
                let! response =
                    match request with
                    | Create  c -> StoreAction.create  c
                    | Read    r -> StoreAction.read    r
                    | Replace r -> StoreAction.replace r
                    | Update  u -> StoreAction.update  u
                    | Delete  d -> StoreAction.delete  d

                channel.Reply response

                return! messageLoop()
            }
            messageLoop())
        (id, writer)

    let dbOf (request: StoreRequest) =
        match request with
        | Create  (db, _, _)    -> db
        | Read    (db, _)       -> db
        | Replace (db, _, _)    -> db
        | Update  (db, _, _, _) -> db
        | Delete  (db, _)       -> db

    let keyOf (request: StoreRequest) =
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

module Store =

    open StoreHelpers
    open TinkerIo

    let private writers =
        seq {
            for id in [0 .. Config.StoreWriters - 1] do
                yield makeWriter id
        } |> dict

    let post (request: StoreRequest) = async {
        let db       = dbOf request
        let key      = keyOf request
        let index    = indexOf Config.StoreWriters db key
        let response = writers.[index].PostAndAsyncReply (fun channel -> request, channel)

        return! response
    }

