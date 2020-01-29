namespace TinkerIo.Stream

open System
open System.Threading

module StreamLock =

    let maxPartitions = 100

    let private partitions =
        [0 .. maxPartitions]
        |> List.map (fun i -> (i, new ManualResetEventSlim()))
        |> dict

    let private indexOf(key: string) =
        key.GetHashCode() % maxPartitions
        |> Math.Abs

    let rec Wait(key: string) (f: unit -> Async<'a option>) : Async<'a> =  async {
        match! f() with
        | Some result -> return result
        | _ ->
            let i = indexOf(key)
            partitions.[i].Reset()
            match! f() with
            | Some result -> return result
            | _ ->
                partitions.[i].Wait()
                return! Wait key f
        }

    let Signal(key: string) =
        let i = indexOf key
        partitions.[i].Set()
