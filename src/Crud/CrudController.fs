namespace TinkerIo.Crud

open Microsoft.AspNetCore.Mvc
open TinkerIo
open Newtonsoft.Json.Linq

type Post = CrudRequest -> Async<CrudResult>

type CrudController(post: Post) =
    inherit ControllerBase()

    // 409 - conflict
    [<HttpPut("create/{db}/{key}")>]
    member __.Create(db: string, key: string, [<FromBody>] content: JRaw) = async {
        let! result = Create(db, key, content.ToString()) |> post
        return Crud.toResponse result
    }

    // 404 - not found
    [<HttpGet("read/{db}/{key}")>]
    member __.Read(db: string, key: string) = async {
        let! result =  Read(db, key) |> post
        return Crud.toResponse result
    }

    [<HttpPut("replace/{db}/{key}")>]
    member __.Replace(db: string, key: string, [<FromBody>] content: JRaw) = async {
        let! result =  Replace(db, key, content.ToString()) |> post
        return Crud.toResponse result
    }

    // 409 - conflict
    [<HttpPut("update/{db}/{key}/{hash}")>]
    member __.Update(db: string, key: string, hash: string, [<FromBody>] content: JRaw) = async {
        let json = content.ToString()
        let! result =  Update(db, key, json, hash) |> post
        return Crud.toResponse result
    }

    [<HttpDelete("delete/{db}/{key}")>]
    member __.Delete(db: string, key: string) = async {
        let! result =  Delete(db, key) |> post
        return Crud.toResponse result
    }

    [<HttpPut("publish/{db}/{key}")>]
    member __.Publish(db: string, key: string, [<FromBody>] content: JRaw) = async {
        let! result = Publish(db, key, content.ToString()) |> post

        Wait.Release (db + key)

        return Crud.toResponse result
    }

    [<HttpGet("subscribe/{db}/{key}/{hash}")>]
    member __.Subscribe(db: string, key: string, hash: string) = async {
        let! result = Wait.Til (db + key) (fun k ->
            async {
                match! Read(db, key) |> post with
                | Success value ->
                    if value.Hash <> hash
                    then return Success value  |> Some
                    else return None
                | Failure value ->
                    if value.Error.Contains("not exist") // todo super lame dude
                    then return None
                    else return Failure value |> Some
            })

        return Crud.toResponse result
    }