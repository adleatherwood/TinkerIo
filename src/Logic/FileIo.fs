namespace TinkerIo

module FileIo =

    open System.IO

    let read filename : AsyncResult<string> = async {
        try
            let! content = File.ReadAllTextAsync(filename) |> Async.AwaitTask
            return Ok content
        with
        | e -> return Error e.Message
    }

    let rec write filename content : AsyncResult<unit> = async {
        try
            File.WriteAllTextAsync(filename, content) |> Async.AwaitTask |> ignore
            return Ok ()
        with
        | :? DirectoryNotFoundException ->
            FileInfo(filename).DirectoryName |> Directory.CreateDirectory |> ignore
            return! write filename content
        | e -> return Error e.Message
    }

    let delete filename : Result<unit> =
        try
            Ok <| File.Delete(filename)
        with
        | e -> Error e.Message
