namespace FSharp.Finance.Personal.Tests

open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal.EquipmentFinance.Depreciation.DepreciationCommon

module DepreciationCommonTests =

    [<Fact>]
    let ``Validate positive amount accepts positive values`` () =
        Validation.validatePositiveAmount 100L<FSharp.Finance.Personal.Calculation.Cent> "test" // Should not throw

    [<Fact>]
    let ``Validate positive amount rejects zero and negative`` () =
        (fun () -> Validation.validatePositiveAmount 0L<FSharp.Finance.Personal.Calculation.Cent> "test") |> should throw typeof<System.Exception>
        (fun () -> Validation.validatePositiveAmount -10L<FSharp.Finance.Personal.Calculation.Cent> "test") |> should throw typeof<System.Exception>

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
        Calculations.calculateRemainingValue 10000L<FSharp.Finance.Personal.Calculation.Cent> 3000L<FSharp.Finance.Personal.Calculation.Cent> |> should equal 7000L<FSharp.Finance.Personal.Calculation.Cent>
        Calculations.calculateRemainingValue 5000L<FSharp.Finance.Personal.Calculation.Cent> 5000L<FSharp.Finance.Personal.Calculation.Cent> |> should equal 0L<FSharp.Finance.Personal.Calculation.Cent>

    [<Fact>]
    let ``Apply rate works correctly`` () =
        Calculations.applyRate 1000L<FSharp.Finance.Personal.Calculation.Cent> 0.18m |> should equal 180L<FSharp.Finance.Personal.Calculation.Cent>
        Calculations.applyRate 5000L<FSharp.Finance.Personal.Calculation.Cent> 0.06m |> should equal 300L<FSharp.Finance.Personal.Calculation.Cent>

    [<Fact>]
    let ``Cap depreciation at cost works`` () =
        let originalCost = 10000L<FSharp.Finance.Personal.Calculation.Cent>
        let cumulative = 5000L<FSharp.Finance.Personal.Calculation.Cent>
        
        // Normal case - no capping needed
        Calculations.capDepreciationAtCost originalCost 1000L<FSharp.Finance.Personal.Calculation.Cent> cumulative |> should equal 1000L<FSharp.Finance.Personal.Calculation.Cent>
        
        // Capping needed - proposed exceeds remaining
        Calculations.capDepreciationAtCost originalCost 6000L<FSharp.Finance.Personal.Calculation.Cent> cumulative |> should equal 5000L<FSharp.Finance.Personal.Calculation.Cent>
        
        // Edge case - exactly at limit
        Calculations.capDepreciationAtCost originalCost 5000L<FSharp.Finance.Personal.Calculation.Cent> cumulative |> should equal 5000L<FSharp.Finance.Personal.Calculation.Cent>

    [<Fact>]
    let ``Educational disclaimer is not empty`` () =
        Disclaimers.EducationalDisclaimer |> should not' (be EmptyString)
        Disclaimers.UKSpecificDisclaimer |> should not' (be EmptyString)
        Disclaimers.USSpecificDisclaimer |> should not' (be EmptyString)