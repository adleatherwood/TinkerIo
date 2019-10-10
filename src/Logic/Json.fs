namespace TinkerIo

open Newtonsoft.Json.Linq

type JsonResponse = {
        Key      : string
        Hash     : string
        Success  : bool
        Message  : string
        Document : JRaw
        }

module Json =

    let convert(response: DbResponse) : JsonResponse =
        match response with
        | Success s -> {Success = true; Message = ""; Key = s.Key; Hash = s.Hash; Document = new JRaw(s.Content)}
        | Failure f -> {Success = false; Message = f.Error; Key = f.Key; Hash = ""; Document = new JRaw("{}")}
