namespace FSharp.Finance.Personal

/// convenience module for generating HTML tables, optimised for amortisation schedules
module Formatting =

    open System
    open System.IO
    open System.Reflection
    open Microsoft.FSharp.Reflection
    open System.Text.RegularExpressions

    open Calculation
    open Currency
 
    /// an array of properties relating to (product) fees
    let feesProperties hide = if hide then [| "FeesPortion"; "FeesRefund"; "FeesBalance"; "FeesRefundIfSettled" |] else [||]
    /// an array of properties relating to (penalty) charges
    let chargesProperties hide = if hide then [| "NewCharges"; "ChargesPortion"; "ChargesBalance" |] else [||]
    /// an array of properties relating to quotes
    let quoteProperties hide = if hide then [| "GeneratedPayment" |] else [||]
    /// an array of properties representing extra information
    let extraProperties hide = if hide then [| "FeesRefundIfSettled"; "SettlementFigure"; "Window" |] else [||]

    /// filter out hidden fields 
    let filterColumns hideProperties =
        match hideProperties with
        | [||] -> id
        | columns -> Array.filter(fun (pi: PropertyInfo) -> columns |> Array.exists(( = ) pi.Name) |> not)

    /// writes some content to a specific file path, creating the containing directory if it does not exist
    let outputToFile filePath content =
        let fi = FileInfo filePath
        if not fi.Directory.Exists then fi.Directory.Create() else ()
        File.WriteAllText(filePath, content)

    /// writes content to a file in the application's IO directory
    let outputToFile' fileName content =
        outputToFile $"{__SOURCE_DIRECTORY__}/../io/{fileName}" content

    let internal regexMetadata = Regex(@"metadata = map \[.*?\]")
    let internal regexObject = Regex(@"[{}]")
    let internal regexType = Regex(@"(scheduled payment type|actual payment status) = ")
    let internal regexSimple = Regex(@"\( simple (.+?)\)")
    let internal regexDate = Regex(@"\d{4}-\d{2}-\d{2}")
    let internal regexFailed = Regex(@"(failed )\((.+?), \[\|.*?\|\]\)")
    let internal regexArray = Regex(@"\[\|(.*?)\|\]")
    let internal regexDecimal = Regex(@"([\d\.]+)M")
    let internal regexInt64 = Regex(@"(\d+)L")
    let internal regexInt32 = Regex(@"(\d+)\b")
    let internal regexNone = Regex(@"(value&nbsp;)?none")
    let internal regexSome = Regex(@"value some \(?([^)]+)\)?")
    let internal regexZero = Regex(@"\b0L\b")
    let internal regexLineReturn = Regex(@"\s*[\r\n]\s*")
    let internal regexWhitespace = Regex(@"\s+")
    let internal regexPascaleCase = Regex(@"(?<=\b|\p{Ll})(\p{Lu})")

    let internal splitPascale s = regexPascaleCase.Replace(s, fun (m: Match) -> $" {m.Groups[0].Value}")

    /// creates a table header row from a record's fields
    let formatHtmlTableHeader hideProperties propertyInfos =
        let thh =
            propertyInfos
            |> filterColumns hideProperties
            |> Array.mapi(fun i pi -> $"""<th class="ci{i}">{splitPascale pi.Name |> (_.Trim().Replace(" ", "&nbsp;"))}</th>""")
            |> String.concat ""
        $"<thead>{thh}</thead>"

    /// writes a table cell, formatting the value for legibility (optimised for amortisation schedules)
    let formatHtmlTableCell item index (propertyInfo: PropertyInfo) =
        item
        |> propertyInfo.GetValue
        |> sprintf "%A"
        |> fun s -> regexPascaleCase.Replace(s, fun (m: Match) -> $" {m.Groups[1].Value |> (_.ToLower())}")
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
            if s |> regexDate.IsMatch then $"""<td class="ci{index} cDateValue" style="white-space: nowrap;">{s}</td>"""
            elif s |> regexZero.IsMatch then $"""<td class="ci{index} cZeroValue" style="color: #808080; text-align: right;">0.00</td>"""
            elif s |> regexDecimal.IsMatch then regexDecimal.Replace(s, fun m -> m.Groups[1].Value |> Convert.ToDecimal |> fun m -> m / 100m |> (_.ToString("N4"))) |> fun s -> $"""<td class="ci{index} cNumberValue" style="text-align: right;">{s}</td>"""
            elif s |> regexInt64.IsMatch then regexInt64.Replace(s, fun m -> m.Groups[1].Value |> Convert.ToInt64 |> ( * ) 1L<Cent> |> Cent.toDecimal |> (_.ToString("N2"))) |> fun s -> $"""<td class="ci{index} cNumberValue" style="text-align: right;">{s}</td>"""
            elif s |> regexInt32.IsMatch then $"""<td class="ci{index} cNumberValue" style="text-align: right;">{s}</td>"""
            elif s |> regexNone.IsMatch then $"""<td class="ci{index} cValueNone">&nbsp;</td>"""
            else $"""<td class="ci{index}">{s}</td>"""

    /// writes table rows from an array
    let formatHtmlTableRows hideProperties propertyInfos items =
        items
        |> Array.map(fun li ->
            propertyInfos
            |> filterColumns hideProperties
            |> Array.mapi (formatHtmlTableCell li)
            |> String.concat ""
            |> fun tdd -> $"<tr>{tdd}</tr>"
        )
        |> String.concat ""

    /// a set of options specifying which fields to show/hide in the output
    type GenerationOptions = {
        GoParameters: PaymentSchedule.Parameters
        GoPurpose: IntendedPurpose
        GoExtra: bool
    }

    /// determines which fields to hide
    let getHideProperties generationOptions =
        match generationOptions with
        | Some go ->
            [|
                go.GoParameters.FeesAndCharges.Fees |> Array.isEmpty |> feesProperties
                go.GoParameters.FeesAndCharges.Charges |> Array.isEmpty |> chargesProperties
                (match go.GoPurpose with IntendedPurpose.Settlement _ -> false | _ -> true) |> quoteProperties
                go.GoExtra |> not |> extraProperties
            |]
            |> Array.concat
        | None ->
            [||]

    /// generates a formatted HTML table from an array
    let generateHtmlFromArray options (items: 'a array) =
        let hideProperties = options |> getHideProperties
        let propertyInfos = typeof<'a> |> FSharpType.GetRecordFields
        let header = propertyInfos |> formatHtmlTableHeader hideProperties
        let rows = items |> formatHtmlTableRows hideProperties propertyInfos
        $"<table>{header}{rows}</table>"

    /// legacy function for creating HTML files from enumerables
    let outputListToHtml fileName list =
        list
        |> generateHtmlFromArray None
        |> outputToFile' fileName

