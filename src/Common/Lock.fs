namespace TinkerIo

open System.Threading
open System.Collections.Concurrent

module private LockHelpers =

    let newLock(_: string) =
        new ManualResetEventSlim()

    let wait(_: string) (lock: ManualResetEventSlim) =
        lock.Reset()
        lock.Wait()
        lock

    let set(_: string) (lock: ManualResetEventSlim) =
        lock.Set()
        lock

module Wait =

    open LockHelpers

    let locks = ConcurrentDictionary<string, ManualResetEventSlim>()

    let For(key: string) =
        locks.AddOrUpdate(key, newLock, wait) |> ignore

    let rec Til(key: string) (f: unit -> Async<'a option>) = async {
        match! f() with
        | Some result -> return result
        | None ->
            For key
            return! Til key f
        }

    let Release(key: string) =
        locks.AddOrUpdate(key, newLock, set)
        |> ignore

