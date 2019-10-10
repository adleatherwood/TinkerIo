namespace DataIo

open System
open System.IO

module md5 =

    open System.Text
    open System.Security.Cryptography

    let hash (input: string) =
        use md5     = MD5.Create()
        let bytes   = System.Text.Encoding.UTF8.GetBytes(input)
        let hashed  = md5.ComputeHash(bytes)
        let builder = StringBuilder()

        for i in [0 .. hashed.Length - 1] do
            let hexed = hashed.[i].ToString("X2")
            builder.Append(hexed) |> ignore

        builder.ToString().ToLower()

module FileIo =

    type Result<'a> = Result<'a, string>
    type AsyncResult<'a> = Async<Result<'a>>

    let WriteAll filename content : AsyncResult<unit> = async {
        try
            File.WriteAllTextAsync(filename, content) |> Async.AwaitTask |> ignore
            return Ok ()
        with
        | e -> return Error e.Message
    }

    let ReadAll filename : AsyncResult<string> = async {
        try
            let! content = File.ReadAllTextAsync(filename) |> Async.AwaitTask
            return Ok content
        with
        | e -> return Error e.Message
    }

    let Delete filename : Result<unit> =
        try
            Ok <| File.Delete(filename)
        with
        | e -> Error e.Message

[<AutoOpen>]
module DbIoTypes =

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

module DbIo =

    // TODO: to some standard linux path for containerization
    let private root =
        Environment.GetEnvironmentVariable("DATAIO_ROOT")
        |> Option.ofObj
        |> Option.defaultValue "./test"

    let toFilename key =
        if String.IsNullOrWhiteSpace(key)
        then None
        else Some <| Path.Combine(root, key)

    let private (|IsNew|_|) =
        Option.filter (File.Exists >> not)

    let private (|IsExisting|_|) =
        Option.filter File.Exists

    let private (|IsCurrent|) (h1: string, h2: string) =
        h1 = h2

    let private toSuccess key content =
        Success {Key = key; Hash = md5.hash content; Content = content}

    let private toFailure key error =
        Failure {Key = key; Error = error}

    let private writeAll filename key content = async {
        let! result = FileIo.WriteAll filename content
        return
            match result with
            | Ok _    -> toSuccess key content
            | Error e -> toFailure key e
    }

    let private readAll filename key = async {
        let! result  = FileIo.ReadAll filename
        return
            match result with
            | Ok content -> toSuccess key content
            | Error e    -> toFailure key e
    }

    let private delete filename key =
        let result = FileIo.Delete filename
        match result with
        | Ok _    -> toSuccess key ""
        | Error e -> toFailure key e

    let Create (key: string, content: string) : Async<DbResponse> = async {
        match toFilename key with
        | IsNew filename -> return! writeAll filename key content
        | _ -> return toFailure key "File already exists"
    }

    let Read (key: string) : Async<DbResponse> = async {
        match toFilename key with
        | IsExisting filename -> return! readAll filename key
        | _ -> return toFailure key "File does not exist"
    }

    let Overwrite (key: string, content: string) : Async<DbResponse> = async {
        match toFilename key with
        | IsExisting filename -> return! writeAll filename key content
        | _ -> return toFailure key "File does not exist"
    }

    let Update (key: string, hash: string, content: string) : Async<DbResponse> = async {
        match! Read key with
        | Failure msg -> return Failure msg
        | Success red ->
            match red.Hash, hash with
            | IsCurrent true -> return! Overwrite (key, content)
            | _ -> return toFailure key "Hash is out of date"
    }

    let Delete (key: string) : Async<DbResponse> = async {
        return
            match toFilename key with
            | IsExisting filename -> delete filename key
            | _ -> toSuccess key ""
    }

[<AutoOpen>]
module DbTypes =

    type DbRequest =
        | Create of (string * string)
        | Read of string
        | Overwrite of (string * string)
        | Update of (string * string * string)
        | Delete of string

module Db =

    type Message = DbRequest * Control.AsyncReplyChannel<DbResponse>

    let private maxWriters = 100

    let private makeWriter id =
        let writer = MailboxProcessor<Message>.Start(fun inbox ->
            let rec messageLoop() = async{
                let! request, channel = inbox.Receive()
                let! response =
                    match request with
                    | Create    c -> DbIo.Create c
                    | Read      r -> DbIo.Read r
                    | Overwrite o -> DbIo.Overwrite o
                    | Update    u -> DbIo.Update u
                    | Delete    d -> DbIo.Delete d

                channel.Reply response

                return! messageLoop()
            }
            messageLoop())
        (id, writer)

    let private writers =
        seq {
        for id in [0 .. maxWriters - 1] do
            yield makeWriter id
        } |> dict

    let private getKey (request: DbRequest) =
        match request with
        | Create    (key, _)    -> key
        | Read       key        -> key
        | Overwrite (key, _)    -> key
        | Update    (key, _, _) -> key
        | Delete     key        -> key

    let private getIndex request =
        let key   = getKey request
        let hash  = key.GetHashCode()
        let index = hash % maxWriters |> Math.Abs
        index

    let Post (request: DbRequest) : Async<DbResponse> = async {
        let index    = getIndex request
        let response = writers.[index].PostAndAsyncReply (fun channel -> request, channel)

        return! response
    }

module Jzon =

    open Newtonsoft.Json.Linq

    type JsonResponse = {
        Key      : string
        Hash     : string
        Success  : bool
        Message  : string
        Document : JRaw
        }

    let Convert(response: DbResponse) : JsonResponse =
        match response with
        | Success s -> {Success = true; Message = ""; Key = s.Key; Hash = s.Hash; Document = new JRaw(s.Content)}
        | Failure f -> {Success = false; Message = f.Error; Key = f.Key; Hash = ""; Document = new JRaw("{}")}
