namespace FSharp.Finance

module Formatting =

    open Microsoft.DotNet.Interactive.Formatting
    open System.IO
    open System.Text.RegularExpressions

    let outputListToHtml fileName limit list =
        Formatter.ListExpansionLimit <- limit |> Option.defaultValue 200
        let formatter = Formatter.GetPreferredFormatterFor(typeof<TabularData.TabularDataResource>, Formatter.DefaultMimeType)
        let trd = TabularData.TabularDataResourceFormatter.ToTabularDataResource list
        let writer = new StringWriter()
        formatter.Format(trd, writer)
        let clean (output: string) = 
            output.Replace(" 00:00:00Z", "").Replace(@" class=""dni-plaintext""", "")
            |> fun s -> Regex.Replace(s, @"<(pre|span)>(.+?)</\1>", "$2")
            |> fun s -> Regex.Replace(s, "<style>.+?</style>", "", RegexOptions.Singleline)
        writer.ToString()
        |> clean
        |> fun s -> File.WriteAllText($"{__SOURCE_DIRECTORY__}/../io/{fileName}", s)
