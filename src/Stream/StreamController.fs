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
        return StreamConvert.toJson result
    }

    [<HttpGet("read/{stream}/{offset}/{max}")>]
    member __.Read(stream: string, offset: uint32, max: uint32) = async {
        let! result = Stream.read stream offset max
        return result
    }
