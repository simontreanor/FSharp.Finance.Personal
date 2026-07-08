namespace FSharp.Finance.Personal.Tests

open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal.Calculation
open FSharp.Finance.Personal.EquipmentFinance.Depreciation.DepreciationCommon

module DepreciationCommonTests =

    [<Fact>]
    let ``Validate positive amount accepts positive values`` () =
        Validation.validatePositiveAmount 100L<Cent> "test" // Should not throw

    [<Fact>]
    let ``Validate positive amount rejects zero and negative`` () =
        (fun () -> Validation.validatePositiveAmount 0L<Cent> "test") |> should throw typeof<System.Exception>
        (fun () -> Validation.validatePositiveAmount -10L<Cent> "test") |> should throw typeof<System.Exception>

    [<Fact>]
    let ``Validate percentage accepts valid range`` () =
        Validation.validatePercentage 0m "test" // Should not throw
        Validation.validatePercentage 0.5m "test" // Should not throw
        Validation.validatePercentage 1m "test" // Should not throw

    [<Fact>]
    let ``Validate percentage rejects invalid range`` () =
        (fun () -> Validation.validatePercentage -0.1m "test") |> should throw typeof<System.Exception>
        (fun () -> Validation.validatePercentage 1.1m "test") |> should throw typeof<System.Exception>

    [<Fact>]
    let ``Calculate remaining value works`` () =
        Calculations.calculateRemainingValue 10000L<Cent> 3000L<Cent> |> should equal 7000L<Cent>
        Calculations.calculateRemainingValue 5000L<Cent> 5000L<Cent> |> should equal 0L<Cent>

    [<Fact>]
    let ``Apply rate works correctly`` () =
        Calculations.applyRate 1000L<Cent> 0.18m |> should equal 180L<Cent>
        Calculations.applyRate 5000L<Cent> 0.06m |> should equal 300L<Cent>

    [<Fact>]
    let ``Cap depreciation at cost works`` () =
        let originalCost = 10000L<Cent>
        let cumulative = 5000L<Cent>
        
        // Normal case - no capping needed
        Calculations.capDepreciationAtCost originalCost 1000L<Cent> cumulative |> should equal 1000L<Cent>
        
        // Capping needed - proposed exceeds remaining
        Calculations.capDepreciationAtCost originalCost 6000L<Cent> cumulative |> should equal 5000L<Cent>
        
        // Edge case - exactly at limit
        Calculations.capDepreciationAtCost originalCost 5000L<Cent> cumulative |> should equal 5000L<Cent>

    [<Fact>]
    let ``Educational disclaimer is not empty`` () =
        Disclaimers.EducationalDisclaimer |> should not' (be EmptyString)
        Disclaimers.UKSpecificDisclaimer |> should not' (be EmptyString)
        Disclaimers.USSpecificDisclaimer |> should not' (be EmptyString)

    [<Fact>]
    let ``Straight-line depreciation sums to cost minus salvage`` () =
        let cost    = 10000_00L<Cent>   // 10,000.00
        let salvage =  1000_00L<Cent>   // 1,000.00
        let life = 5

        let schedule = Calculations.straightLine cost salvage life
        schedule.Length |> should equal life

        let totalDep =
            schedule |> Array.sumBy (fun p -> p.Depreciation)

        totalDep |> should equal (cost - salvage)

        // Final book value equals salvage
        let last = schedule |> Array.last
        last.BookValue |> should equal salvage
        // Monotonic decline
        schedule |> Array.pairwise |> Array.iter (fun (a,b) ->
            b.BookValue |> should be (lessThanOrEqualTo a.BookValue))

        // All methods flagged SL
        schedule |> Array.iter (fun p -> p.Method |> should equal "SL")


    [<Fact>]
    let ``Declining balance without switch reaches salvage and never goes below`` () =
        let cost    = 20000_00L<Cent>   // 20,000.00
        let salvage =  2000_00L<Cent>   // 2,000.00
        let life = 8
        let rateFactor = 2.0m

        let schedule = Calculations.decliningBalance cost salvage life rateFactor false
        schedule.Length |> should equal life

        let final = schedule |> Array.last
        final.BookValue |> should equal salvage

        // Never below salvage
        schedule |> Array.iter (fun p -> p.BookValue |> should be (greaterThanOrEqualTo salvage))

        // Rate-based periods are labelled "DB"; the final period is a plug to land exactly
        // on salvage and is labelled distinctly
        schedule
        |> Array.take (schedule.Length - 1)
        |> Array.iter (fun p -> p.Method |> should equal "DB")
        (schedule |> Array.last).Method |> should equal "DB (final adjustment)"

        // Sum depreciation = cost - salvage
        let totalDep = schedule |> Array.sumBy (fun p -> p.Depreciation)
        totalDep |> should equal (cost - salvage)


    [<Fact>]
    let ``Declining balance switches to straight-line for optimal write-off`` () =
        let cost    = 15000_00L<Cent>
        let salvage =  500_00L<Cent>
        let life = 6
        let rateFactor = 2.0m

        let schedule = Calculations.decliningBalance cost salvage life rateFactor true
        schedule.Length |> should equal life

        // Ensure at least one period uses switch method
        schedule |> Array.exists (fun p -> p.Method = "DB->SL") |> should equal true

        let final = Array.last schedule
        final.BookValue |> should equal salvage

        // Sum depreciation = cost - salvage
        let totalDep = schedule |> Array.sumBy (fun p -> p.Depreciation)
        totalDep |> should equal (cost - salvage)

        // Monotonic decline
        schedule |> Array.pairwise |> Array.iter (fun (a,b) ->
            b.BookValue |> should be (lessThanOrEqualTo a.BookValue))

    [<Fact>]
    let ``Straight-line final adjustment keeps salvage exact despite rounding`` () =
        // Choose numbers that cause repeating decimals
        let cost    = 9999_99L<Cent>
        let salvage =   123_45L<Cent>
        let life = 7

        let schedule = Calculations.straightLine cost salvage life
        let final = schedule |> Array.last
        final.BookValue |> should equal salvage

        // Total depreciation matches cost - salvage
        schedule |> Array.sumBy (fun p -> p.Depreciation)
        |> should equal (cost - salvage)

    [<Fact>]
    let ``Declining balance never depreciates below salvage mid-schedule`` () =
        let cost    = 8000_00L<Cent>
        let salvage = 1000_00L<Cent>
        let life = 10
        let rateFactor = 2.0m

        let schedule = Calculations.decliningBalance cost salvage life rateFactor true

        schedule
        |> Array.take (schedule.Length - 1)
        |> Array.iter (fun p -> p.BookValue |> should be (greaterThan salvage))

        (Array.last schedule).BookValue |> should equal salvage