namespace TinkerIo

module Hash =

    open System.Text
    open System.Security.Cryptography

    let private md5 = MD5.Create()

    let create (input: string) =
        let builder = StringBuilder(input.Length * 2)

        System.Text.Encoding.UTF8.GetBytes(input)
        |> md5.ComputeHash
        |> Array.iter (sprintf "%02X" >> String.toLower >> builder.Append >> ignore)

        builder.ToString()
