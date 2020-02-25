namespace TinkerIo.Topic

open Microsoft.AspNetCore.Mvc
open TinkerIo.Source

[<ApiController>]
[<Route("[controller]")>]
type TopicController() =
    inherit SourceController(Topic.post)

