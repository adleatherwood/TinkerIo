namespace TinkerIo.Stream

type StreamResponse = {
    Offset: uint32
    Success: bool
    Message: string
}

module StreamConvert =

    let toJson (result: AppendResult) =
        match result with
        | AppendSuccess offset  -> {Offset=offset; Success=true; Message=""}
        | AppendFailure message -> {Offset=0u; Success=false; Message=message}
