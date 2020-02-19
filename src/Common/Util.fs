namespace TinkerIo

open System
open System.Collections.Generic

[<AutoOpen>]
module Util =

    type Result<'a> = Result<'a, string>

    type AsyncResult<'a> = Async<Result<'a>>

module String =

    let toLower (s: string) =
        s.ToLower()

    let splitSnd (splitter: char) (s: string) =
        s.Split(splitter).[1]

module StringCi =

    let startsWith (find: string) (s: string) =
        s.StartsWith(find, StringComparison.CurrentCultureIgnoreCase)

module Dict =

    let addOrUpdate (d: Dictionary<'k,'v>) (key: 'k, add: 'k -> 'v, update: 'k -> 'v -> 'v) =
        let mutable (found, value) = d.TryGetValue key
        let newValue =
            if found
            then update key value
            else add key
        d.[key] <- newValue
        newValue