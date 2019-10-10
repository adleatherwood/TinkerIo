namespace TinkerIo

open System

type Key = string
type Content = string
type HashCode = string

type DbRequest =
    | Create  of (Key * Content)
    | Read    of  Key
    | Replace of (Key * Content)
    | Update  of (Key * Content * HashCode)
    | Delete  of  Key

module private DbHelpers =

    type Message = DbRequest * Control.AsyncReplyChannel<DbResponse>

    let defaultWriters = 1000

    let overrideWriters =
        Environment.GetEnvironmentVariable("TINKERIO_MAX_WRITERS")
        |> Option.ofObj
        |> Option.map Int32.Parse

    let containeredWriters =
        Environment.GetEnvironmentVariable("TINKERIO_CONTAINERED")
        |> Option.ofObj
        |> Option.map (fun _ -> 100)

    let maxWriters =
        overrideWriters
        |> Option.orElse containeredWriters
        |> Option.defaultValue defaultWriters

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

    let keyOf (request: DbRequest) =
        match request with
        | Create  (key, _)    -> key
        | Read     key        -> key
        | Replace (key, _)    -> key
        | Update  (key, _, _) -> key
        | Delete   key        -> key

    let indexOf maxWriters key =
        let hash  = key.GetHashCode()
        let index = hash % maxWriters |> Math.Abs
        index

module Db =

    open DbHelpers

    let private writers =
        seq {
            for id in [0 .. maxWriters - 1] do
                yield makeWriter id
        } |> dict

    let post (request: DbRequest) : Async<DbResponse> = async {
        let key      = keyOf request
        let index    = indexOf maxWriters key
        let response = writers.[index].PostAndAsyncReply (fun channel -> request, channel)

        return! response
    }

