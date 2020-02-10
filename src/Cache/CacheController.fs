namespace TinkerIo.Cache

open Microsoft.AspNetCore.Mvc
open TinkerIo
open Newtonsoft.Json.Linq

[<ApiController>]
[<Route("[controller]")>]
type CacheController() =
    inherit ControllerBase()

    // todo duplicative

    [<HttpPut("create/{db}/{key}")>]
    member __.Create(db: string, key: string, [<FromBody>] content: JRaw) = async {
        let! reply = Create(db, key, content.ToString()) |> Cache.post
        return CacheConvert.toJson reply
    }

    [<HttpGet("read/{db}/{key}")>]
    member __.Read(db: string, key: string) = async {
        let! reply =  Read(db, key) |> Cache.post
        return CacheConvert.toJson reply
    }

    [<HttpPut("replace/{db}/{key}")>]
    member __.Replace(db: string, key: string, [<FromBody>] content: JRaw) = async {
        let! reply =  Replace(db, key, content.ToString()) |> Cache.post
        return CacheConvert.toJson reply
    }

    [<HttpPut("update/{db}/{key}/{hash}")>]
    member __.Update(db: string, key: string, hash: string, [<FromBody>] content: JRaw) = async {
        let json = content.ToString()
        let! reply =  Update(db, key, json, hash) |> Cache.post
        return CacheConvert.toJson reply
    }

    [<HttpDelete("delete/{db}/{key}")>]
    member __.Delete(db: string, key: string) = async {
        let! reply =  Delete(db, key) |> Cache.post
        return CacheConvert.toJson reply
    }

    [<HttpPut("publish/{db}/{key}")>]
    member __.Publish(db: string, key: string, [<FromBody>] content: JRaw) = async {
        let! reply = Publish(db, key, content.ToString()) |> Cache.post

        Wait.Release (db + key)

        return CacheConvert.toJson reply
    }

    [<HttpGet("subscribe/{db}/{key}/{hash}")>]
    member __.Subscribe(db: string, key: string, hash: string) = async {
        let! result = Wait.Til (db + key) (fun k ->
            async {
                match! Read(db, key) |> Cache.post with
                | Success value ->
                    if value.Hash <> hash
                    then return Success value  |> Some
                    else return None
                | Failure value -> return Failure value |> Some
            })

        return CacheConvert.toJson result
    }