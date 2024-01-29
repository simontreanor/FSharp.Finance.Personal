namespace FSharp.Finance.Personal

module Formatting =

    open System
    open System.IO
    open System.Reflection
    open Microsoft.FSharp.Reflection
    open System.Text.RegularExpressions
 
    let outputToFile filePath content =
        let fi = FileInfo filePath
        if not fi.Directory.Exists then fi.Directory.Create() else ()
        File.WriteAllText(filePath, content)

    let outputToFile' fileName content =
        outputToFile $"{__SOURCE_DIRECTORY__}/../io/{fileName}" content

    let formatHtmlTableHeader (propertyInfos: PropertyInfo array) =
        let thh = propertyInfos |> Array.map(fun pi -> $"<th>{pi.Name}</th>") |> String.concat ""
        $"<thead>{thh}</thead>"

    let regexInt64 = Regex(@"(\d+)L")
    let regexInt32 = Regex(@"(\d+)\b")
    let regexNone = Regex(@"ValueNone")
    let regexSome = Regex(@"ValueSome (\w+)")
    let regexEmptyArray = Regex(@"[||]")

    let formatHtmlTableCell item (propertyInfo: PropertyInfo) =
        item
        |> propertyInfo.GetValue
        |> sprintf "%A"
        |> fun s -> 
            if s |> regexInt64.IsMatch then regexInt64.Replace(s, fun m -> m.Groups[1].Value |> Convert.ToInt64 |> ( * ) 1L<Cent> |> Cent.toDecimal |> (_.ToString("N2"))) |> fun s -> $"""<td style="text-align: right;">{s}</td>"""
            elif s |> regexInt32.IsMatch then $"""<td style="text-align: right;">{s}</td>"""
            elif s |> regexNone.IsMatch then """<td>&nbsp;</td>"""
            elif s |> regexSome.IsMatch then $"""<td>{regexSome.Replace(s, "$1")}</td>"""
            elif s |> regexEmptyArray.IsMatch then """<td>&nbsp;</td>"""
            else $"<td>{s}</td>"

    // to-do: why are advances zero? format non-empty arrays with pluses concatenating?

    let formatHtmlTableRows propertyInfos items =
        items
        |> Array.map(fun li ->
            propertyInfos
            |> Array.map (formatHtmlTableCell li)
            |> String.concat ""
            |> fun tdd -> $"<tr>{tdd}</tr>"
        )
        |> String.concat ""
        
    let generateHtmlFromList (items: 'a array) =
        let propertyInfos = typeof<'a> |> FSharpType.GetRecordFields
        let header = propertyInfos |> formatHtmlTableHeader
        let rows = items |> formatHtmlTableRows propertyInfos
        $"<table>{header}{rows}</table>"

    let outputListToHtml fileName list =
        list
        |> generateHtmlFromList
        |> outputToFile' fileName
