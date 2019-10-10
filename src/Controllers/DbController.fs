namespace TinkerIo.Controllers

open Microsoft.AspNetCore.Mvc
open TinkerIo
open Newtonsoft.Json.Linq

[<ApiController>]
[<Route("[controller]")>]
type DbController() = //(logger : ILogger<DbController>) =
    inherit ControllerBase()

    [<HttpPut("create/{key}")>]
    member __.Create(key: string, [<FromBody>] content: JRaw) = async {
        let! reply = Create (key, content.ToString()) |> Db.post
        return Json.convert reply
    }

    [<HttpGet("read/{key}")>]
    member __.Read(key: string) = async {
        let! reply =  Read key |> Db.post
        return Json.convert reply
    }
