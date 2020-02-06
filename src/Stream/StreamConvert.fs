namespace TinkerIo.Stream

open System
open Newtonsoft.Json
open Newtonsoft.Json.Linq

type JsonAppendResponse = {
    Stream : string
    Offset : uint32
}

type JsonReadResponse = {
    Stream  : string
    Next    : uint32
    IsEnd   : bool
    Entries : Entry[]
}

type JsonFailureResponse = {
    Stream : string
    Error  : string
}

module StreamConvert =

    let toJson (response: Response) =
        match response with
        | Append (stream, offset)  -> JRaw.FromObject { Stream=stream; Offset=offset}
        | Read (stream, next, isEnd, entries) -> JRaw.FromObject {Stream=stream; Next=next; IsEnd=isEnd; Entries=entries}
        | Failure (stream, error) -> JRaw.FromObject {Stream=stream; Error=error}
