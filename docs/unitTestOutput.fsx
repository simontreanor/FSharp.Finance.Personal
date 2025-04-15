(**
---
title: Unit-Test Output
category: Output
categoryindex: 4
index: 1
description: Simple schedules and amortisation schedules output by the unit tests
keywords: payment APR charge edge-case illustrative interest schedule promotional quote settlement unit-period config
---
*)

(**
# Unit-Tests Output

<style>
details {
    margin-bottom: 1rem;
}
summary {
    padding: 0.5rem 1rem;
    cursor: pointer;
    font-weight: bold;
}
details table {
    width: 100%;
    border-collapse: collapse;
}
details table td {
    padding: 0.5rem;
    border-top: 1px solid #eee;
}
</style>
*)

(*** hide ***)
#r "System.IO"
#r "System.Text.RegularExpressions"

open System.IO
open System.Text.RegularExpressions

let descriptionPattern = Regex "<p><h4>Description</h4><i>(.+?)</i></p>"

Path.Combine(__SOURCE_DIRECTORY__, "..", "io", "out")
|> Directory.EnumerateDirectories
|> Seq.map(fun directoryPath ->
    let directoryName = Path.GetFileName directoryPath
    let filesRows = 
        directoryPath
        |> Directory.EnumerateFiles
        |> Seq.map(fun filePath ->
            let fileName = Path.GetFileName filePath
            let description =
                let m = File.ReadAllText filePath |> descriptionPattern.Match
                if m.Success then
                    m.Groups[1].Value
                else
                    "(no description)"
            $"""<tr><td><a href="/FSharp.Finance.Personal/content/{directoryName}/{fileName}" target="{fileName}">{fileName}</a></td><td>{description}</td></tr>"""
        )
        |> String.concat ""
    $"""<details>
        <summary>{directoryName}</summary>
        <table>
            <tr><td><b>Test</b></td><td><b>Description</b></td></tr>
            {filesRows}
        </table>
    </details>"""
)
|> String.concat ""

(*** include-it-raw ***)