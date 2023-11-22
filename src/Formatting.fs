namespace FSharp.Finance.Personal

module Formatting =

    open Microsoft.DotNet.Interactive.Formatting
    open System.IO
    open System.Text.RegularExpressions
    open System.Linq

    /// creates a formatted HTML table from an IEnumerable
    let outputListToHtml fileName limit list =
        Formatter.ListExpansionLimit <- limit |> ValueOption.defaultValue 200
        Formatter.RecursionLimit <- 1
        let formatter = Formatter.GetPreferredFormatterFor(typeof<TabularData.TabularDataResource>, Formatter.DefaultMimeType)
        let trd = TabularData.TabularDataResourceFormatter.ToTabularDataResource list
        let writer = new StringWriter()
        formatter.Format(trd, writer)
        let clean (output: string) = 
            output.Replace(" 00:00:00Z", "").Replace(@" class=""dni-plaintext""", "")
            |> fun s -> Regex.Replace(s, @"<div>(.+?)</div>", "$1")
            |> fun s -> Regex.Replace(s, @"<pre>(.+?)</pre>", "$1")
            |> fun s -> Regex.Replace(s, @"<span>(.+?)</span>", "$1")
            |> fun s -> Regex.Replace(s, @"<style.+?</style>", "", RegexOptions.Singleline)
            |> fun s -> Regex.Replace(s, @"<details.+?<code>(.+?)</code>.+?</details>", "$1", RegexOptions.Singleline)
            |> fun s ->
                Regex.Replace(s, 
                    @"<td><table><thead><tr><th><i>index</i></th><th>value</th></tr></thead><tbody><tr><td>\d+</td><td>.+?</td></tr>+</tbody></table></td>",
                    fun m ->
                        Regex.Matches(m.Value, @"<tr><td>\d+</td><td>(.+?)</td></tr>").Cast<Match>()
                        |> Seq.map _.Groups[1].Value
                        |> String.concat "; "
                        |> fun s -> $"<td>{s}</td>"
                )
        writer.ToString()
        |> clean
        |> fun s -> File.WriteAllText($"{__SOURCE_DIRECTORY__}/../io/{fileName}", s)
