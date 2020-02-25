namespace TinkerIo.Stream

open Microsoft.AspNetCore.Mvc
open TinkerIo.Source

[<ApiController>]
[<Route("[controller]")>]
type StreamController() =
    inherit SourceController(Stream.post)

    // todo
    // [<HttpPost("clearAll")>]
    // member __.ClearAll() = async {
    //     Io.documents.Clear()

    //     return {Success = true; Message = "Cached cleard";}
    // }