namespace TinkerIo.Store

open Microsoft.AspNetCore.Mvc
open TinkerIo.Crud

[<ApiController>]
[<Route("[controller]")>]
type StoreController() =
    inherit CrudController(Store.post)
