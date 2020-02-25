namespace TinkerIo.Crud

module private CrudLang =

    type Db       = string
    type Key      = string
    type Hash     = string
    type Location = string
    type Content  = string
    type Problem  = string

open System
open CrudLang
open Newtonsoft.Json.Linq
open TinkerIo

type CrudIo = {
    Location : Db -> Key  -> Location option
    Exists   : Location -> bool
    Write    : Location -> Content -> AsyncResult<Unit>
    Read     : Location -> AsyncResult<Content>
    Delete   : Location -> Result<Unit>
}

type CrudRequest =
    | Create  of (Db * Key * Content)
    | Read    of (Db * Key)
    | Replace of (Db * Key * Content)
    | Update  of (Db * Key * Content * Hash)
    | Delete  of (Db * Key)
    | Publish of (Db * Key * Content)

type CrudSuccess = {
    Key      : string
    Hash     : string
    Content  : string
    }

type CrudFailure = {
    Key      : string
    Error    : string
    }

type CrudResult =
    | Success of CrudSuccess
    | Failure of CrudFailure

type CrudResponse = {
    Key      : string
    Hash     : string
    Success  : bool
    Message  : string
    Document : JRaw
    }

type CrudMessage = {
    Success : bool
    Message : string
}

module private CrudHelpers =

    let toSuccess key content =
        Success {Key = key; Hash = Hash.create content; Content = content}

    let toFailure key error =
        Failure {Key = key; Error = error}

    let (|IsNew|_|) io =
        Option.filter (io.Exists >> not)

    let (|IsExisting|_|) io =
        Option.filter io.Exists

    let (|IsCurrent|_|) (h1: string, h2: string) =
        if h1 = h2
        then Some h2
        else None

    let write io location key content = async {
        match! io.Write location content with
        | Ok _    -> return toSuccess key content
        | Error e -> return toFailure key e
    }

    let read io location key = async {
        match! io.Read location with
        | Ok content -> return toSuccess key content
        | Error e    -> return toFailure key e
    }

    let delete io location key =
        match io.Delete location with
        | Ok _    -> toSuccess key ""
        | Error e -> toFailure key e

module Crud =

    open CrudHelpers

    let create (io: CrudIo) (db: string, key: string, content: string) = async {
        match io.Location db key with
        | IsNew io location -> return! write io location key content
        | _ -> return toFailure key "Document already exists"
    }

    let read (io: CrudIo) (db: string, key: string) = async {
        match io.Location db key with
        | IsExisting io location -> return! read io location key
        | _ -> return toFailure key "Document does not exist"
    }

    let replace (io: CrudIo) (db: string, key: string, content: string) = async {
        match io.Location db key with
        | IsExisting io location -> return! write io location key content
        | _ -> return toFailure key "Document does not exist"
    }

    let update (io: CrudIo) (db: string, key: string, content: string, hash: string) = async {
        match! read io (db, key) with
        | Failure msg -> return Failure msg
        | Success red ->
            match red.Hash, hash with
            | IsCurrent _ -> return! replace io (db, key, content)
            | _ -> return toFailure key "Hash is out of date"
    }

    let delete (io: CrudIo) (db: string, key: string) = async {
        match io.Location db key with
        | IsExisting io location -> return delete io location key
        | _ -> return toSuccess key ""
    }

    let publish (io: CrudIo) (db: string, key: string, content: string) = async {
        match io.Location db key with
        | Some location -> return! write io location key content
        | _ -> return toFailure key "Invalid db or key value"
    }

    let post (io: CrudIo) (request: CrudRequest) = async {
        let! response =
            match request with
            | Create  c -> create  io c
            | Read    r -> read    io r
            | Replace r -> replace io r
            | Update  u -> update  io u
            | Delete  d -> delete  io d
            | Publish p -> publish io p
        return response
    }

    let toResponse(result: CrudResult) : CrudResponse =
        match result with
        | Success s ->
            let content =
                if String.IsNullOrWhiteSpace(s.Content)
                then "{}"
                else s.Content

            {Success = true; Message = ""; Key = s.Key; Hash = s.Hash; Document = new JRaw(content)}
        | Failure f ->
            {Success = false; Message = f.Error; Key = f.Key; Hash = ""; Document = new JRaw("{}")}
