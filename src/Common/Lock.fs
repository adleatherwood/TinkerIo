namespace TinkerIo.Stream

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

module Lock =

    open LockHelpers

    let locks = ConcurrentDictionary<string, ManualResetEventSlim>()

    let rec WaitTil(key: string) (f: unit -> Async<'a option>) : Async<'a> =  async {
        match! f() with
        | Some result -> return result
        | None ->
            locks.AddOrUpdate(key, newLock, wait) |> ignore
            return! WaitTil key f
        }

    let Signal(key: string) =
        locks.AddOrUpdate(key, newLock, set)
        |> ignore

