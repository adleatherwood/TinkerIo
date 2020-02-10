namespace TinkerIo.Stream

open TinkerIo
open Microsoft.AspNetCore.Mvc
open Newtonsoft.Json.Linq

[<ApiController>]
[<Route("[controller]")>]
type StreamController() =
    inherit ControllerBase()

    [<HttpPost("append/{stream}")>]
    member __.Append(stream: string, [<FromBody>] content: JRaw) = async {
        let request = Request.Append (stream, content.ToString())
        let! result = Stream.post request

        Wait.Release stream

        return StreamConvert.toJson result
    }

    [<HttpGet("read/{stream}/{offset}/{count}")>]
    member __.Read(stream: string, offset: uint32, count: uint32) = async {
        let! result = Wait.Til stream (fun () -> async {
            let request = Request.Read (stream, offset, count)
            let! result = Stream.post request
            match result with
            | Read (_,_,_,entries) ->
                if entries.Length > 0
                then return Some result
                else return None
            | Response.Failure f -> return Some result
            | _ -> return None
        })

        return StreamConvert.toJson result
    }
