namespace TinkerIo

open System

[<AutoOpen>]
module Util =

    type Result<'a> = Result<'a, string>

    type AsyncResult<'a> = Async<Result<'a>>

module String =

    let toLower (s: string) =
        s.ToLower()

    let startsWithCi (find: string) (s: string) =
        s.StartsWith(find, StringComparison.CurrentCultureIgnoreCase)

    let splitSnd (splitter: char) (s: string) =
        s.Split(splitter).[1]