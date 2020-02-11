namespace TinkerIo.Cache

open Microsoft.AspNetCore.Mvc
open TinkerIo.Crud


[<ApiController>]
[<Route("[controller]")>]
type CacheController() =
    inherit CrudController(Cache.post)
