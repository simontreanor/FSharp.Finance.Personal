namespace FSharp.Finance.Personal

/// convenience module for generating HTML tables, optimised for amortisation schedules
module Formatting =

    open System
    open System.IO
    open System.Reflection
    open Microsoft.FSharp.Reflection
    open System.Text.RegularExpressions
 
    /// writes some content to a specific file path, creating the containing directory if it does not exist
    let outputToFile filePath content =
        let fi = FileInfo filePath
        if not fi.Directory.Exists then fi.Directory.Create() else ()
        File.WriteAllText(filePath, content)

    /// writes content to a file in the application's IO directory
    let outputToFile' fileName content =
        outputToFile $"{__SOURCE_DIRECTORY__}/../io/{fileName}" content

    /// creates a table header row from a record's fields
    let formatHtmlTableHeader (propertyInfos: PropertyInfo array) =
        let thh = propertyInfos |> Array.map(fun pi -> $"<th>{pi.Name}</th>") |> String.concat ""
        $"<thead>{thh}</thead>"

    let internal regexSimple = Regex(@"\(Simple (.+?)\)")
    let internal regexDate = Regex(@"\d{4}-\d{2}-\d{2}")
    let internal regexArray = Regex(@"\[\|(.*?)\|\]")
    let internal regexInt64 = Regex(@"(\d+)L")
    let internal regexInt32 = Regex(@"(\d+)\b")
    let internal regexNone = Regex(@"ValueNone")
    let internal regexSome = Regex(@"ValueSome \(?([^)]+)\)?")
    let internal regexZero = Regex(@"\b0L\b")

    /// writes a table cell, formatting the value for legibility (optimised for amortisation schedules)
    let formatHtmlTableCell item (propertyInfo: PropertyInfo) =
        item
        |> propertyInfo.GetValue
        |> sprintf "%A"
        |> fun s -> if s |> regexArray.IsMatch then regexArray.Replace(s, "$1") else s
        |> fun s -> if s |> regexSimple.IsMatch then regexSimple.Replace(s, "$1") else s
        |> fun s -> if s |> regexSome.IsMatch then regexSome.Replace(s, "$1") else s
        |> _.Replace(" ", "&nbsp;")
        |> fun s ->
            if s |> regexDate.IsMatch then $"""<td style="white-space: nowrap;">{s}</td>"""
            elif s |> regexZero.IsMatch then """<td style="color: #808080; text-align: right;">0.00</td>"""
            elif s |> regexInt64.IsMatch then regexInt64.Replace(s, fun m -> m.Groups[1].Value |> Convert.ToInt64 |> ( * ) 1L<Cent> |> Cent.toDecimal |> (_.ToString("N2"))) |> fun s -> $"""<td style="text-align: right;">{s}</td>"""
            elif s |> regexInt32.IsMatch then $"""<td style="text-align: right;">{s}</td>"""
            elif s |> regexNone.IsMatch then """<td>&nbsp;</td>"""
            else $"<td>{s}</td>"

    /// writes table rows from an array
    let formatHtmlTableRows propertyInfos items =
        items
        |> Array.map(fun li ->
            propertyInfos
            |> Array.map (formatHtmlTableCell li)
            |> String.concat ""
            |> fun tdd -> $"<tr>{tdd}</tr>"
        )
        |> String.concat ""
        
    /// generates a formatted HTML table from an array
    let generateHtmlFromArray (items: 'a array) =
        let propertyInfos = typeof<'a> |> FSharpType.GetRecordFields
        let header = propertyInfos |> formatHtmlTableHeader
        let rows = items |> formatHtmlTableRows propertyInfos
        $"<table>{header}{rows}</table>"

    /// legacy function for creating HTML files from enumerables
    let outputListToHtml fileName list =
        list
        |> generateHtmlFromArray
        |> outputToFile' fileName
