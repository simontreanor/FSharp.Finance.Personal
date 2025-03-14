namespace FSharp.Finance.Personal

/// convenience module for generating HTML tables, optimised for amortisation schedules
module Formatting =

    open System.IO
    open System.Reflection
    open Microsoft.FSharp.Reflection
    open System.Text.RegularExpressions

    open Calculation

    /// filter out hidden fields 
    let internal filterColumns hideProperties =
        match hideProperties with
        | [||] -> id
        | columns -> Array.filter(fun (pi: PropertyInfo) -> columns |> Array.exists(( = ) pi.Name) |> not)

    /// writes some content to a specific file path, creating the containing directory if it does not exist
    let outputToFile filePath append content =
        let fi = FileInfo filePath
        if not fi.Directory.Exists then
            fi.Directory.Create()
            File.WriteAllText(filePath, content)
        else
            if append then
                File.AppendAllText(filePath, content)
            else
                File.WriteAllText(filePath, content)

    /// writes content to a file in the application's IO directory
    let outputToFile' fileName append content =
        outputToFile $"{__SOURCE_DIRECTORY__}/../io/{fileName}" append content

    let internal regexPascaleCase = Regex @"(?<=\b|\p{Ll})(\p{Lu})"
    let internal splitPascale s = regexPascaleCase.Replace(s, fun (m: Match) -> $" {m.Groups[0].Value}")

    /// creates a table header row from a record's fields
    let formatHtmlTableHeader (indexName: string voption) names =
        let addIndexHeader, indexOffset = if indexName.IsSome then Array.append [| $"""<th class="ci0">{indexName.Value}</th>""" |], 1 else id, 0
        let thh =
            names
            |> Array.mapi(fun i name -> $"""<th class="ci{i + indexOffset}">{splitPascale name |> _.Trim()}</th>""")
            |> addIndexHeader
            |> String.concat ""
        $"<thead>{thh}</thead>"

    let internal formatCent = Cent.toDecimal >> _.ToString("N2")
    let internal formatDecimalCent (m: decimal<Cent>) = decimal m / 100m |> _.ToString("N4")
    let internal formatDecimal m = m / 100m |> _.ToString("N4")
    let internal formatInt64 l = decimal l / 100m |> _.ToString("N2")

    /// writes a table cell, formatting the value for legibility (optimised for amortisation schedules)
    let internal formatHtmlTableCell (index: int) value =
        $"""<td class="ci{index.ToString "00"}">{value}</td>"""

    /// writes table rows from an array
    let internal formatHtmlTableRows hideProperties propertyInfos items =
        items
        |> Array.map(fun li ->
            propertyInfos
            |> filterColumns hideProperties
            |> Array.mapi(fun i pi -> formatHtmlTableCell i (pi.GetValue li))
            |> String.concat ""
            |> fun tdd -> $"<tr>{tdd}</tr>"
        )
        |> String.concat ""

    /// generates a formatted HTML table from an array
    let generateHtmlFromArray hideProperties (items: 'a array) =
        let propertyInfos = typeof<'a> |> FSharpType.GetRecordFields
        let header = propertyInfos |> filterColumns hideProperties |> fun pii -> formatHtmlTableHeader ValueNone (pii |> Array.map _.Name)
        let rows = items |> formatHtmlTableRows hideProperties propertyInfos
        $"<table>{header}{rows}</table>"

    /// creates HTML files from an array
    let outputArrayToHtml fileName append data =
        data |> generateHtmlFromArray [||] |> outputToFile' fileName append
    
    /// writes table rows from a map
    let internal formatHtmlTableRowsFromMap hideProperties propertyInfos data =
        data
        |> Map.map(fun itemIndex li ->
            propertyInfos
            |> filterColumns hideProperties
            |> Array.mapi (fun propertyIndex pi -> formatHtmlTableCell (propertyIndex + 1) (pi.GetValue li))
            |> Array.append [| formatHtmlTableCell 0 itemIndex |]
            |> String.concat ""
            |> fun tdd -> $"<tr>{tdd}</tr>"
        )
        |> Map.values
        |> String.concat ""

    /// generates a formatted HTML table from a map
    let generateHtmlFromMap' hideProperties indexName (data: Map<'a, 'b>) =
        let propertyInfos = typeof<'b> |> FSharpType.GetRecordFields
        let header = propertyInfos |> filterColumns hideProperties |> fun pii -> formatHtmlTableHeader (ValueSome indexName) (pii |> Array.map _.Name)
        let rows = data |> formatHtmlTableRowsFromMap hideProperties propertyInfos
        $"<table>{header}{rows}</table>"

    /// generates a formatted HTML table from a map with the index name "Day"
    let generateHtmlFromMap hideProperties (data: Map<'a, 'b>) =
        generateHtmlFromMap' hideProperties "Day" data

    /// creates HTML files from a map
    let outputMapToHtml' fileName append indexName data =
        data |> generateHtmlFromMap' [||] indexName |> outputToFile' fileName append

    /// creates HTML files from a map with the index name "Day"
    let outputMapToHtml fileName append data =
        outputMapToHtml' fileName append "Day" data
