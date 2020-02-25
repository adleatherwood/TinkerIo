namespace TinkerIo.Source

open Newtonsoft.Json.Linq
open TinkerIo

module private StreamLang =

    type Stream    = string
    type Location<'a>  = 'a
    type Content   = string
    type Error     = string
    type Offset    = uint32
    type Next      = uint32
    type Partition = uint32
    type Count     = uint32
    type IsEnd     = bool

open StreamLang

type SourceRequest =
    | Append of (Stream * Content)
    | Read   of (Stream * Offset * Count)

type SourceEntry = {
    Offset    : uint32
    IsEnd     : bool
    Error     : string
    Document  : JRaw
    }

type SourceResult =
    | Appended of (Stream * Offset)
    | Red      of (Stream * Next * IsEnd * SourceEntry[])
    | Failure  of (Stream * Error)

type SourceAppendResponse = {
    Stream : string
    Offset : uint32
}

type SourceReadResponse = {
    Stream  : string
    Next    : uint32
    IsEnd   : bool
    Entries : SourceEntry[]
}

type SourceFailureResponse = {
    Stream : string
    Error  : string
}

type SourceIo<'a> = {
    Location : Stream -> Offset -> Location<'a>
    Exists   : Location<'a> -> bool
    Next     : Stream -> Offset
    Last     : Stream -> Offset
    Write    : Location<'a> -> Content -> AsyncResult<Unit>
    Read     : Location<'a> -> AsyncResult<Content>
    Commit   : Stream -> Offset -> Unit
}

type SourceMessage = {
    Success : bool
    Message : string
}

module private Action =

    let traverse(io: SourceIo<'a>) (stream: string) (start: uint32) (max: uint32) =
        seq {
            let mutable count = 0u
            let mutable current = start
            let last = io.Last stream

            while (count < max && current <= last) do
                let location = io.Location stream current
                let isEnd = current = last
                if io.Exists location then
                    count <- count + 1u
                    yield (isEnd, current, location)
                current <- current + 1u
        } |> Seq.toArray

    let toEntry(io: SourceIo<'a>) (isEnd: bool, index: uint32, location: 'a)  =
        async {
            match! io.Read location with
            | Ok content    -> return {Offset=index; Document=JRaw content; IsEnd=isEnd; Error=null}
            | Error message -> return {Offset=index; Document=null; IsEnd=isEnd; Error=message}
        }

    let append (io: SourceIo<'a>) (stream: string, content: string) = async {
        let offset = io.Next(stream)
        let location = io.Location stream offset
        let! result = io.Write location content

        io.Commit stream offset

        return
            match result with
            | Ok _        -> Appended (stream, offset)
            | Error error -> Failure (stream, error)
    }

    let read (io: SourceIo<'a>) (stream: string, offset: uint32, count: uint32) = async {
        let! entries =
            traverse io stream offset count
            |> Array.map (toEntry io)
            |> Async.Sequential

        let last = Array.tryLast entries
        let (isEnd, next) =
            match last with
            | Some entry -> (entry.IsEnd, entry.Offset + 1u)
            | None -> (true, offset)

        return Red (stream, next, isEnd, entries)
    }

module Source =

    let post (io: SourceIo<'a>) (request: SourceRequest) = async {
        let! response =
            match request with
            | Append a -> Action.append io a
            | Read   r -> Action.read   io r

        return response
    }


    let toResponse (response: SourceResult) =
        match response with
        | Appended (stream, offset)  -> JRaw.FromObject { Stream=stream; Offset=offset}
        | Red (stream, next, isEnd, entries) -> JRaw.FromObject {Stream=stream; Next=next; IsEnd=isEnd; Entries=entries}
        | Failure (stream, error) -> JRaw.FromObject {Stream=stream; Error=error}
