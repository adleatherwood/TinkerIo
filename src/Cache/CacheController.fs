namespace TinkerIo.Cache

open Microsoft.AspNetCore.Mvc
open TinkerIo.Crud
open Newtonsoft.Json.Linq

[<ApiController>]
[<Route("[controller]")>]
type CacheController() =
    inherit CrudController(Cache.post)

    [<HttpPost("clearAll")>]
    member __.ClearAll() = async {
        Io.documents.Clear()

        return {Success = true; Message = "Cached cleared"}
    }
