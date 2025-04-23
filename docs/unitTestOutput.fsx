(**
---
title: Unit-Test Outputs
category: Output
categoryindex: 5
index: 1
description: Simple schedules and amortisation schedules output by the unit tests
keywords: payment APR charge edge-case illustrative interest schedule promotional quote settlement unit-period config
---
*)

(**
# Unit-Test Outputs

Each of the following categories contains a number of unit-test outputs along with the parameters used and some statistics.
Click on a category to expand it.
<br />
<br />

> Note: load any schedule table into Excel by following these steps:
> 
> 1. Click **Data** > **From Web**
> 2. Paste the URL of the schedule page and click **OK**
> 3. In the Navigator dialog, select the relevant table from HTML tables
> 4. Click **Load**

*)

(*** hide ***)
#r "System.IO"
#r "System.Text.RegularExpressions"

open System.IO
open System.Text.RegularExpressions

let descriptionPattern = Regex "<h4>Description</h4>\s*<p><i>(.+?)</i></p>"

Path.Combine(__SOURCE_DIRECTORY__, "..", "io", "out")
|> Directory.EnumerateDirectories
|> Seq.sort
|> Seq.map(fun directoryPath ->
    let directoryName = Path.GetFileName directoryPath
    let filesRows = 
        directoryPath
        |> Directory.EnumerateFiles
        |> Seq.sort
        |> Seq.map(fun filePath ->
            let fileName = Path.GetFileNameWithoutExtension filePath
            let description =
                let m = File.ReadAllText filePath |> descriptionPattern.Match
                if m.Success then
                    m.Groups[1].Value
                else
                    "(no description)"
            $"""<tr><td><a href="content/{directoryName}/{fileName}.html" target="{fileName}">{fileName}</a></td><td>{description}</td></tr>"""
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