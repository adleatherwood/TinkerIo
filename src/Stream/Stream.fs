namespace TinkerIo.Stream

open System
open System.IO
open System.Collections.Concurrent
open Newtonsoft.Json.Linq
open TinkerIo
open System.Collections.Generic


type Stream    = string
type Content   = string
type Error     = string
type Offset    = uint32
type Next      = uint32
type Partition = uint32
type Count     = uint32
type IsEnd     = bool

type Request =
    | Append of (Stream * Content)
    | Read   of (Stream * Offset * Count)

type Entry = {
    // todo fix casing
    Offset    : uint32
    IsEnd     : bool
    Error     : string
    Document  : JRaw
    }

type Response =
    | Append  of (Stream * Offset)
    | Read    of (Stream * Next * IsEnd * Entry[])
    | Failure of (Stream * Error)


module private Index =

    let private lastFile(folder: string) =
        Directory.EnumerateFiles(folder)
        |> Seq.map (Path.GetFileName >> UInt32.Parse)
        |> Seq.max

    let private streams =
        Directory.CreateDirectory(Config.StreamRoot).FullName
        |> Directory.EnumerateDirectories
        |> Seq.map (DirectoryInfo >> (fun stream -> (stream.Name, lastFile stream.FullName)))
        |> dict
        |> ConcurrentDictionary<string, uint32>

    let private committed =
        streams :> IEnumerable<KeyValuePair<string,uint32>>
        |> Seq.map (fun kvp -> (kvp.Key, [kvp.Value]))
        |> dict
        |> ConcurrentDictionary<string, uint32 list>

    let private padWidth =
        UInt32.MaxValue
            .ToString()
            .Length

    let private toFilename(i: uint32) =
        i.ToString().PadLeft(padWidth, '0')

    let toFilepath (stream: string) (index: uint32) =
        let filename = toFilename index
        Path.Combine(Config.StreamRoot, stream, filename)

    let next (stream: string) =
        streams.AddOrUpdate(stream, 0u, fun _ current -> current + 1u)

    let last (stream: string) =
        match committed.TryGetValue(stream) with
        | (true, list) -> if list.Length > 0 then list.[0] else 0u
        | _ -> 0u
        //streams.GetOrAdd(stream, 0u)

    let private compress(list: uint32 list) =
        let sorted = List.sort list
        let compressed =
            list
            |> List.sort
            |> List.mapi (fun i offset -> if i = 0 then (offset, offset - 1u) else (offset, sorted.[i-1]))
            |> List.skipWhile (fun (a,b) -> a - b = 1u)
            |> List.map fst

        match compressed with
        | [] -> [List.last sorted]
        | _ -> compressed

    let commit(stream: string) (offset: uint32) : unit =
        committed.AddOrUpdate(stream, (fun _ -> [0u]), (fun _ list -> offset :: list |> compress)) |> ignore
        //StreamLock.Signal stream


    let traverse(stream: string) (start: uint32) (max: uint32) =
        seq {
            let mutable count = 0u
            let mutable current = start
            let last = last(stream)

            while (count < max && current <= last) do
                let filepath = toFilepath stream current
                let isEnd = current = last
                if File.Exists(filepath) then
                    count <- count + 1u
                    yield (isEnd, current, filepath)
                current <- current + 1u
        } |> Seq.toArray

    let toEntry(isEnd: bool, index: uint32, filepath: string)  =
        async {
            match! FileIo.read filepath with
            | Ok content    -> return {Offset=index; Document=JRaw content; IsEnd=isEnd; Error=null}
            | Error message -> return {Offset=index; Document=null; IsEnd=isEnd; Error=message}
        }

module private StreamAction =

    let Append(stream: string, content: string) = async {
        let index = Index.next(stream) // todo rename offset
        let filename = Index.toFilepath stream index
        let! result = FileIo.write filename content

        Index.commit stream index

        return
            match result with
            | Ok _        -> Append  (stream, index)
            | Error error -> Failure (stream, error)
    }

    let Read(stream: string, offset: uint32, count: uint32) = async {
        let! entries =
            Index.traverse stream offset count
            |> Array.map Index.toEntry
            |> Async.Sequential

        let last = Array.tryLast entries //indexes
        let (isEnd, next) =
            match last with
            | Some entry -> (entry.IsEnd, entry.Offset + 1u)
            | None -> (true, offset)

        return Read (stream, next, isEnd, entries)
    }

module private StreamHelpers =

    type Message = Request * Control.AsyncReplyChannel<Response>

    // let makeWorker id =
    //     let writer = MailboxProcessor<Message>.Start(fun inbox ->
    //         let rec messageLoop() = async{
    //             let! request, channel = inbox.Receive()
    //             let! response =
    //                 match request with
    //                 | Request.Append a -> StreamAction.Append a
    //                 | Request.Read   r -> StreamAction.Read   r

    //             channel.Reply response

    //             return! messageLoop()
    //         }
    //         messageLoop())
    //     (id, writer)

    // let private roundup(offset: Offset) : Partition =
    //     let d = decimal offset
    //     Math.Floor((d + 10m) / 10m) * 10m |> uint32

    let streamOf (request: Request) =
        match request with
        | Request.Append (stream, _)    -> stream
        | Request.Read   (stream, _, _) -> stream

    // let partitionOf (request: Request) : Partition =
    //     match request with
    //     | Request.Append (stream, _)        -> Index.last stream |> roundup
    //     | Request.Read   (_, offset, count) -> offset + count - 1u |> roundup

    // let indexOf (workers: int) (stream: Stream) (partition: Partition) =
    //     let hash  = (sprintf "%s%i" stream partition).GetHashCode()
    //     let index = hash % workers |> Math.Abs
    //     index

module Stream =

    open StreamHelpers

    // let private workers =
    //     seq {
    //         // todo: make separate config
    //         for id in [0 .. Config.StoreWriters - 1] do
    //             yield makeWorker id
    //     } |> dict

    let post (request: Request) = async {
        // let stream    = streamOf request
        // let partition = partitionOf request
        // let index     = indexOf Config.StoreWriters stream partition
        // let response  = workers.[index].PostAndAsyncReply (fun channel -> request, channel)
        let! response =
            match request with
            | Request.Append a -> StreamAction.Append a
            | Request.Read   r -> StreamAction.Read   r

        return response
    }
