namespace FSharp.Finance.Personal

/// convenience module for generating HTML tables, optimised for amortisation schedules
module Formatting =

    open System.IO
    open System.Reflection
    open Microsoft.FSharp.Reflection
    open System.Text.RegularExpressions

    open Calculation

    /// writes some content to a specific file path, creating the containing directory if it does not exist
    let outputToFile filePath append content =
        let fi = FileInfo filePath
        Directory.CreateDirectory fi.DirectoryName |> ignore

        if append then
            File.AppendAllText(filePath, content)
        else
            File.WriteAllText(filePath, content)

    /// writes content to a file in the application's IO directory
    let outputToFile' fileName append content =
        outputToFile $"{__SOURCE_DIRECTORY__}/../io/{fileName}" append content

    let internal regexPascaleCase = Regex @"(?<=\b|\p{Ll})(\p{Lu})"

    let internal splitPascale s =
        regexPascaleCase.Replace(s, fun (m: Match) -> $" {m.Groups[0].Value}")

    /// creates a table header row from a record's fields
    let formatHtmlTableHeader (indexName: string voption) names =
        let addIndexHeader, indexOffset =
            if indexName.IsSome then
                Array.append [| $"""<th class="ci0">{indexName.Value}</th>""" |], 1
            else
                id, 0

        let thh =
            names
            |> Array.mapi (fun i name -> $"""<th class="ci{i + indexOffset}">{splitPascale name |> _.Trim()}</th>""")
            |> addIndexHeader
            |> String.concat ""

        $"<thead>{thh}</thead>"

    let internal formatDecimalCent (m: decimal<Cent>) = decimal m / 100m |> _.ToString("N4")
    let internal formatDecimal m = m / 100m |> _.ToString("N4")
    let internal formatInt64 l = decimal l / 100m |> _.ToString("N2")

    /// writes a table cell, formatting the value for legibility (optimised for amortisation schedules)
    let internal formatHtmlTableCell (index: int) value =
        $"""<td class="ci{index:``00``}">{value}</td>"""

    /// writes table rows from an array
    let internal formatHtmlTableRows (propertyInfos: PropertyInfo array) items =
        items
        |> Array.map (fun li ->
            propertyInfos
            |> Array.mapi (fun i pi -> formatHtmlTableCell i (pi.GetValue li))
            |> String.concat ""
            |> fun tdd -> $"<tr>{tdd}</tr>"
        )
        |> String.concat ""

    /// generates a formatted HTML table from an array
    let generateHtmlFromArray (items: 'a array) =
        let propertyInfos = typeof<'a> |> FSharpType.GetRecordFields

        let header =
            propertyInfos
            |> fun pii -> formatHtmlTableHeader ValueNone (pii |> Array.map _.Name)

        let rows = items |> formatHtmlTableRows propertyInfos

        $"""
<table>
    {header}
    {rows}
</table>
"""
