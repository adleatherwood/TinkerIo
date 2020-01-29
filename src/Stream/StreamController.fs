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
        let! result = Stream.append stream (content.ToString())

        StreamLock.Signal stream

        return StreamConvert.toJson result
    }

    [<HttpGet("read/{stream}/{offset}/{max}")>]
    member __.Read(stream: string, offset: uint32, max: uint32) = async {
        let! result = StreamLock.Wait stream (fun () ->
            async {
                let! r = Stream.read stream offset max
                if r.Entries.Length = 0
                then return None
                else return Some r
            })

        return result
    }
