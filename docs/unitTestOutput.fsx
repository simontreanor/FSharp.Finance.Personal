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
# Unit-Test Output
*)

(*** hide ***)
#r "System.IO"
#r "System.Text.RegularExpressions"

open System.IO
open System.Text.RegularExpressions

let descriptionPattern = Regex "<p><h4>Description</h4><i>(.+?)</i></p>"

// Generate the documentation content 
let content =
    Path.Combine(__SOURCE_DIRECTORY__, "..", "io", "out")
    |> Directory.EnumerateDirectories
    |> Seq.map(fun directoryPath ->
        let directoryName = Path.GetFileName directoryPath
        
        let filesTable = 
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
                
                $"| [{fileName}](/io/out/{directoryName}/{fileName}) | {description} |"
            )
            |> String.concat "\n"
        
        $"## {directoryName}\n\n| File | Description |\n| --- | --- |\n{filesTable}\n\n"
    )
    |> String.concat "\n"

printf "%s" content

(*** include-output-raw ***)