namespace FSharp.Finance.Personal

/// convenience module for generating HTML tables, optimised for amortisation schedules
module Formatting =

    open System
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

    let internal regexMetadata = Regex(@"(metadata = )?map \[.*?\]", RegexOptions.IgnoreCase)
    let internal regexObject = Regex @"[{}]"
    let internal regexType = Regex(@"(scheduled payment type|actual payment status) = ", RegexOptions.IgnoreCase)
    let internal regexSimple = Regex(@"\( simple (.+?)\)", RegexOptions.IgnoreCase)
    let internal regexDate = Regex @"\d{4}-\d{2}-\d{2}"
    let internal regexFailed = Regex(@"(failed )\((.+?), \[\|.*?\|\]\)", RegexOptions.IgnoreCase)
    let internal regexArray = Regex @"\[\|(.*?)\|\]"
    let internal regexZeroM = Regex(@"\b0M\b", RegexOptions.IgnoreCase)
    let internal regexDecimal = Regex(@"([\d\.]+)M", RegexOptions.IgnoreCase)
    let internal regexInt64 = Regex(@"(\d+)L", RegexOptions.IgnoreCase)
    let internal regexInt32 = Regex @"(\d+)\b"
    let internal regexNone = Regex(@"(value&nbsp;)?none", RegexOptions.IgnoreCase)
    let internal regexSome = Regex(@"value some \(?([^)]+)\)?", RegexOptions.IgnoreCase)
    let internal regexZero = Regex(@"\b0L\b", RegexOptions.IgnoreCase)
    let internal regexLineReturn = Regex @"\s*[\r\n]\s*"
    let internal regexWhitespace = Regex @"\s+"
    let internal regexPascaleCase = Regex @"(?<=\b|\p{Ll})(\p{Lu})"

    let internal splitPascale s = regexPascaleCase.Replace(s, fun (m: Match) -> $" {m.Groups[0].Value}")

    /// creates a table header row from a record's fields
    let formatHtmlTableHeader (indexName: string voption) names =
        let addIndexHeader, indexOffset = if indexName.IsSome then Array.append [| $"""<th class="ci0">{indexName.Value}</th>""" |], 1 else id, 0
        let thh =
            names
            |> Array.mapi(fun i name -> $"""<th class="ci{i + indexOffset}">{splitPascale name |> (_.Trim().Replace(" ", "&nbsp;"))}</th>""")
            |> addIndexHeader
            |> String.concat ""
        $"<thead>{thh}</thead>"

    let internal formatCent = Cent.toDecimal >> (_.ToString("N2"))
    let internal formatDecimal c = c / 100m |> (_.ToString("N4"))

    let internal formatCell index className style value = $"""<td class="ci{index} {className}" style="{style}">{value}</td>"""

    /// writes a table cell, formatting the value for legibility (optimised for amortisation schedules)
    let internal formatHtmlTableCell index value =
        value
        |> sprintf "%A"
        |> fun s -> regexPascaleCase.Replace(s, fun (m: Match) -> $" {m.Groups[1].Value |> (_.ToLower())}").TrimStart()
        |> fun s -> if s |> regexObject.IsMatch then regexObject.Replace(s, "") else s
        |> fun s -> if s |> regexType.IsMatch then regexType.Replace(s, "") else s
        |> fun s -> if s |> regexMetadata.IsMatch then regexMetadata.Replace(s, "") else s
        |> fun s -> if s |> regexLineReturn.IsMatch then regexLineReturn.Replace(s, " ") else s
        |> fun s -> if s |> regexFailed.IsMatch then regexFailed.Replace(s, "$1$2") else s
        |> fun s -> if s |> regexSimple.IsMatch then regexSimple.Replace(s, "$1") else s
        |> fun s -> if s |> regexArray.IsMatch then regexArray.Replace(s, "$1") else s
        |> fun s -> if s |> regexSome.IsMatch then regexSome.Replace(s, "$1") else s
        |> fun s -> if s |> regexWhitespace.IsMatch then regexWhitespace.Replace(s, "&nbsp;") else s
        |> fun s ->
            if s |> regexDate.IsMatch then s |> formatCell index "cDateValue" "white-space: nowrap;"
            elif s |> regexZero.IsMatch then formatCell index "cZeroValue" "color: #808080; text-align: right;" "0.00"
            elif s |> regexZeroM.IsMatch then formatCell index "cZeroValue" "color: #808080; text-align: right;" "0.0000"
            elif s |> regexDecimal.IsMatch then regexDecimal.Replace(s, fun m -> m.Groups[1].Value |> Convert.ToDecimal |> formatDecimal) |> formatCell index "cNumberValue" "text-align: right;"
            elif s |> regexInt64.IsMatch then regexInt64.Replace(s, fun m -> m.Groups[1].Value |> Convert.ToInt64 |> ( * ) 1L<Cent> |> formatCent) |> formatCell index "cNumberValue" "text-align: right;"
            elif s |> regexInt32.IsMatch then s |> formatCell index "cNumberValue" "text-align: right;"
            elif s |> regexNone.IsMatch then formatCell index "cValueNone" "" "&nbsp;"
            else s |> formatCell index "" ""

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
