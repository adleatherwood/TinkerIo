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
        let offset = Index.next(stream)
        let filename = Index.toFilepath stream offset
        let! result = FileIo.write filename content

        Index.commit stream offset

        return
            match result with
            | Ok _        -> Append  (stream, offset)
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

    let streamOf (request: Request) =
        match request with
        | Request.Append (stream, _)    -> stream
        | Request.Read   (stream, _, _) -> stream

module Stream =

    let post (request: Request) = async {
        let! response =
            match request with
            | Request.Append a -> StreamAction.Append a
            | Request.Read   r -> StreamAction.Read   r

        return response
    }
