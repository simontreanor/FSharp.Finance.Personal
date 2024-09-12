namespace FSharp.Finance.Personal

/// convenience module for generating HTML tables, optimised for amortisation schedules
module FormattingHelper =

    open Calculation
 
    /// an array of properties relating to (product) fees
    let feesProperties hide = if hide then [| "FeesPortion"; "FeesRefund"; "FeesBalance"; "FeesRefundIfSettled" |] else [||]
    /// an array of properties relating to (penalty) charges
    let chargesProperties hide = if hide then [| "NewCharges"; "ChargesPortion"; "ChargesBalance" |] else [||]
    /// an array of properties relating to quotes
    let quoteProperties hide = if hide then [| "GeneratedPayment" |] else [||]
    /// an array of properties representing extra information
    let extraProperties hide = if hide then [| "FeesRefundIfSettled"; "SettlementFigure"; "Window" |] else [||]
    /// an array of properties representing internal interest calculations
    let interestProperties hide = if hide then [| "ContractualInterest"; "SimpleInterest"; "OriginalSimpleInterest" |] else [||]

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
                (match go.GoParameters.Interest.Method with Interest.Method.AddOn -> false | _ -> true) |> interestProperties
                go.GoExtra |> not |> extraProperties
            |]
            |> Array.concat
        | None ->
            [||]
