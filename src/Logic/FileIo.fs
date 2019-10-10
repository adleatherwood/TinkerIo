namespace TinkerIo

module FileIo =

    open System.IO

    let write filename content : AsyncResult<unit> = async {
        try
            File.WriteAllTextAsync(filename, content) |> Async.AwaitTask |> ignore
            return Ok ()
        with
        | e -> return Error e.Message
    }

    let read filename : AsyncResult<string> = async {
        try
            let! content = File.ReadAllTextAsync(filename) |> Async.AwaitTask
            return Ok content
        with
        | e -> return Error e.Message
    }

    let delete filename : Result<unit> =
        try
            Ok <| File.Delete(filename)
        with
        | e -> Error e.Message
