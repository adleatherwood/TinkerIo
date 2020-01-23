namespace TinkerIo.Store

open System
open Newtonsoft.Json.Linq

type JsonResponse = {
    Key      : string
    Hash     : string
    Success  : bool
    Message  : string
    Document : JRaw
    }

module StoreConvert =

    let toJson(result: StoreResult) : JsonResponse =
        match result with
        | Success s ->
            let content =
                if String.IsNullOrWhiteSpace(s.Content)
                then "{}"
                else s.Content

            {Success = true; Message = ""; Key = s.Key; Hash = s.Hash; Document = new JRaw(content)}
        | Failure f ->
            {Success = false; Message = f.Error; Key = f.Key; Hash = ""; Document = new JRaw("{}")}
