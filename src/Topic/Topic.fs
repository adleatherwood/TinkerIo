namespace TinkerIo.Topic

open System
open System.IO
open System.Collections.Generic
open System.Collections.Concurrent
open TinkerIo
open TinkerIo.Source

module FileIo =

    let read filename = async {
        try
            let! content = File.ReadAllTextAsync(filename) |> Async.AwaitTask
            return Ok content
        with
        | e -> return Error e.Message
    }

    let rec write filename content = async {
        try
            File.WriteAllTextAsync(filename, content) |> Async.AwaitTask |> ignore
            return Ok ()
        with
        | :? DirectoryNotFoundException ->
            FileInfo(filename).DirectoryName |> Directory.CreateDirectory |> ignore
            return! write filename content
        | e -> return Error e.Message
    }

module Io =

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

    let location (stream: string) (index: uint32) =
        let filename = toFilename index
        Path.Combine(Config.StreamRoot, stream, filename)

    let next (stream: string) =
        streams.AddOrUpdate(stream, 0u, fun _ current -> current + 1u)

    let last (stream: string) =
        match committed.TryGetValue(stream) with
        | (true, list) -> if list.Length > 0 then list.[0] else 0u
        | _ -> 0u

    let commit(stream: string) (offset: uint32) : unit =
        committed.AddOrUpdate(stream, (fun _ -> [0u]), (fun _ list -> offset :: list |> compress)) |> ignore

    let Services : SourceIo<string> =  {
        Location = location
        Exists   = File.Exists
        Next     = next
        Last     = last
        Write    = FileIo.write
        Read     = FileIo.read
        Commit   = commit
    }

module Topic =

    let post = Source.post Io.Services
