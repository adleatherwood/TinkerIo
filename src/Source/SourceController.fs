namespace TinkerIo.Source

open TinkerIo
open Microsoft.AspNetCore.Mvc
open Newtonsoft.Json.Linq

type Post = SourceRequest -> Async<SourceResult>

type SourceController(post: Post) =
    inherit ControllerBase()

    [<HttpPost("append/{stream}")>]
    member __.Append(stream: string, [<FromBody>] content: JRaw) = async {
        let request = Append (stream, content.ToString())
        let! result = post request

        Wait.Release stream

        return Source.toResponse result
    }

    [<HttpGet("read/{stream}/{offset}/{count}")>]
    member __.Read(stream: string, offset: uint32, count: uint32) = async {
        let! result = Wait.Til stream (fun () -> async {
            let request = Read (stream, offset, count)
            let! result = post request
            match result with
            | Red (_,_,_,entries) ->
                if entries.Length > 0
                then return Some result
                else return None
            | Failure f -> return Some result
            | _ -> return None
        })

        return Source.toResponse result
    }
