namespace TinkerIo.Controllers

open Microsoft.AspNetCore.Mvc
open TinkerIo
open Newtonsoft.Json.Linq

[<ApiController>]
[<Route("[controller]")>]
type DbController() =
    inherit ControllerBase()

    [<HttpPost("create/{db}/{key}")>]
    member __.Create(db: string, key: string, [<FromBody>] content: JRaw) = async {
        let! reply = Create(db, key, content.ToString()) |> Db.post
        return Json.convert reply
    }

    [<HttpGet("read/{db}/{key}")>]
    member __.Read(db: string, key: string) = async {
        let! reply =  Read(db, key) |> Db.post
        return Json.convert reply
    }

    [<HttpPost("replace/{db}/{key}")>]
    member __.Replace(db: string, key: string, [<FromBody>] content: JRaw) = async {
        let! reply =  Replace(db, key, content.ToString()) |> Db.post
        return Json.convert reply
    }

    [<HttpPost("update/{db}/{key}/{hash}")>]
    member __.Update(db: string, key: string, hash: string, [<FromBody>] content: JRaw) = async {
        let json = content.ToString()
        let! reply =  Update(db, key, json, hash) |> Db.post

        return Json.convert reply
    }

    [<HttpGet("delete/{db}/{key}")>]
    member __.Delete(db: string, key: string) = async {
        let! reply =  Delete(db, key) |> Db.post
        return Json.convert reply
    }