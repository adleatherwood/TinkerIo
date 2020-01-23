namespace TinkerIo.Store

open Microsoft.AspNetCore.Mvc
open TinkerIo
open Newtonsoft.Json.Linq

[<ApiController>]
[<Route("[controller]")>]
type StoreController() =
    inherit ControllerBase()

    // 409 - conflict
    [<HttpPut("create/{db}/{key}")>]
    member __.Create(db: string, key: string, [<FromBody>] content: JRaw) = async {
        let! reply = Create(db, key, content.ToString()) |> Store.post
        return StoreConvert.toJson reply
    }

    // 404 - not found
    [<HttpGet("read/{db}/{key}")>]
    member __.Read(db: string, key: string) = async {
        let! reply =  Read(db, key) |> Store.post
        return StoreConvert.toJson reply
    }

    [<HttpPut("replace/{db}/{key}")>]
    member __.Replace(db: string, key: string, [<FromBody>] content: JRaw) = async {
        let! reply =  Replace(db, key, content.ToString()) |> Store.post
        return StoreConvert.toJson reply
    }

    // 409 - conflict
    [<HttpPut("update/{db}/{key}/{hash}")>]
    member __.Update(db: string, key: string, hash: string, [<FromBody>] content: JRaw) = async {
        let json = content.ToString()
        let! reply =  Update(db, key, json, hash) |> Store.post
        return StoreConvert.toJson reply
    }

    [<HttpDelete("delete/{db}/{key}")>]
    member __.Delete(db: string, key: string) = async {
        let! reply =  Delete(db, key) |> Store.post
        return StoreConvert.toJson reply
    }