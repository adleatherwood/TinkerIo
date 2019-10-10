namespace TinkerIo

[<AutoOpen>]
module Util =

    type Result<'a> = Result<'a, string>

    type AsyncResult<'a> = Async<Result<'a>>
