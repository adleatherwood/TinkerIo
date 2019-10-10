namespace TinkerIo

module Hash =

    open System.Text
    open System.Security.Cryptography

    let create (input: string) =
        use md5     = MD5.Create()
        let bytes   = System.Text.Encoding.UTF8.GetBytes(input)
        let hashed  = md5.ComputeHash(bytes)
        let builder = StringBuilder()

        for i in [0 .. hashed.Length - 1] do
            let hexed = hashed.[i].ToString("X2")
            builder.Append(hexed) |> ignore

        builder.ToString().ToLower()