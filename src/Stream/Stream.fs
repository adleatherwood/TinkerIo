namespace TinkerIo.Stream

open System
open System.IO
open System.Collections.Concurrent
open Newtonsoft.Json.Linq
open TinkerIo


type StreamEntry = {
    Offset: uint32
    Document : JRaw
    IsEnd: bool
}

type ReadResult= {
   Entries: StreamEntry[]
   Next: uint32
   IsEnd: bool
}

type AppendResult =
    | AppendSuccess of uint32
    | AppendFailure of string

module Index =

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
        streams.GetOrAdd(stream, 0u)

    let read(stream: string) (start: uint32) (max: uint32) : (bool * uint32)[] =
        seq {
            let mutable count = 0u
            let mutable current = start
            let last = last(stream)

            while (count < max && current <= last) do
                let filepath = toFilepath stream current
                let isEnd = current = last
                if File.Exists(filepath) then
                    count <- count + 1u
                    yield (isEnd, current)
                current <- current + 1u

        } |> Seq.toArray

    let withPath (stream: string) (isEnd: bool, index: uint32) =
        (isEnd, index, toFilepath stream index)

    let toEntry(isEnd: bool, index: uint32, filepath: string)  =
        async {
            let! document = File.ReadAllTextAsync(filepath) |> Async.AwaitTask
            return {Offset=index; Document=JRaw document; IsEnd=isEnd}
        }

module Stream =

    let append (stream: string) (content: string) = async {
        let index = Index.next(stream)
        let filename = Index.toFilepath stream index
        let! result = FileIo.write filename content
        return
            match result with
            | Ok _ -> AppendSuccess index
            | Error error  -> AppendFailure error
    }

    let read (stream: string) (start: uint32) (max: uint32) = async {
        let indexes = Index.read stream start max
        let! entries =
            indexes
            |> Array.map (Index.withPath stream >> Index.toEntry)
            |> Async.Sequential

        let last = Array.tryLast indexes
        let (isEnd, next) =
            match last with
            | Some (isEnd, index) -> (isEnd, index + 1u)
            | None -> (true, start)

        return {
            Entries = entries
            Next = next
            IsEnd = isEnd
        }
    }
