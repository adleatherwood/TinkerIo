namespace TinkerIo.Stream

open Microsoft.AspNetCore.Mvc
open TinkerIo.Source

[<ApiController>]
[<Route("[controller]")>]
type StreamController() =
    inherit SourceController(Stream.post)

