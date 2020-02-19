namespace TinkerIo.Stream

open System
open System.Collections.Generic
open System.Collections.Concurrent
open TinkerIo
open TinkerIo.Source
open Newtonsoft.Json.Linq

type Stream = string

type StreamInfo = {
    Last  : uint32
    List  : SourceEntry list
    Red   : SourceEntry list
    IsEnd : bool
    }

module Action =

    let private toi (u: uint32) =
        u |> int

    let private least(a: int) (b: int) =
        if a <= b
        then a
        else b

    let private lasti (list: 'a list) =
        list.Length |> uint32

    let private emptyStream (_: Stream) =
        let list = []
        let info = { Last=0u; List=list; Red=[]; IsEnd=true }
        info

    let private initStream (content: string) (_: Stream) =
        let entry = { Offset=0u; IsEnd=false;Error=null; Document=JRaw content }
        let list = [ entry ]
        let info = { Last=0u; List=list; Red=[]; IsEnd=false }
        info

    let private appendStream (content: string) (_: Stream) (info: StreamInfo) =
        let offset = lasti info.List
        let entry = { Offset=offset; IsEnd=false;Error=null; Document=JRaw content }
        let list = info.List @ [ entry ]
        { Last=offset; List=list; Red=[]; IsEnd=false }

    let private readStream (offset: uint32) (count: uint32) (_: Stream) (info: StreamInfo) =
        let first = toi offset
        let last  = first + (toi count)
        let isEnd = last >= (toi info.Last)
        let red   =
            if first >= info.List.Length
            then []
            elif last >= info.List.Length
            then info.List.GetSlice(Some first, None)
            else info.List.GetSlice(Some first, Some last)
        { info with Red=red; IsEnd=isEnd }

    let append (streams: Dictionary<Stream, StreamInfo>) (stream: string, content: string) = //async {
        let result = Dict.addOrUpdate streams (stream, initStream content, appendStream content)
        (stream, result.Last)

    let read (streams: Dictionary<Stream, StreamInfo>) (stream: string, offset: uint32, count: uint32) =
        let result = Dict.addOrUpdate streams (stream, emptyStream, readStream offset count)
        let next = result.Last + 1u
        let last = result.Last
        let red =
            result.Red
            |> List.map (fun s -> if s.Offset = last then {s with IsEnd=true} else s)
            |> List.toArray
        (stream, next, result.IsEnd, red)

module StreamHelpers =

    type Message = SourceRequest * Control.AsyncReplyChannel<SourceResult>

    let makeWriter id =
        let streams = Dictionary<Stream, StreamInfo>()
        let writer = MailboxProcessor<Message>.Start(fun inbox ->
            let rec messageLoop() = async{
                let! request, channel = inbox.Receive()
                let result =
                    match request with
                    | Append a -> Action.append streams a |> Appended
                    | Read   r -> Action.read   streams r |> Red

                channel.Reply result

                return! messageLoop()
            }
            messageLoop())
        (id, writer)

    let indexOf maxWriters stream =
        let hash  = stream.GetHashCode()
        let index = hash % maxWriters |> Math.Abs
        index

    let streamOf (request: SourceRequest) : Stream =
        match request with
        | Append (stream, _)    -> stream
        | Read   (stream, _, _) -> stream

module Stream =

    open StreamHelpers

    let private writers =
        seq {
            // todo make new config value?
            for id in [0 .. Config.StoreWriters - 1] do
                yield makeWriter id
        } |> dict

    // let post = Source.post Io.Services
    let post (request: SourceRequest) : Async<SourceResult> = async {
        let stream = streamOf request
        let index = indexOf Config.StoreWriters stream
        let result =
            writers.[index].PostAndAsyncReply (fun channel -> request, channel)

        return! result
    }
