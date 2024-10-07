namespace FSharp.Finance.Personal

/// convenience module for generating HTML tables, optimised for amortisation schedules
module FormattingHelper =

    open Calculation

    let asi = Unchecked.defaultof<Amortisation.ScheduleItem>
 
    /// an array of properties relating to (product) fees
    let feesProperties hide =
        if hide then [|
            nameof asi.FeesPortion
            nameof asi.FeesRefund
            nameof asi.FeesBalance
            nameof asi.FeesRefundIfSettled
        |] else [||]
    /// an array of properties relating to (penalty) charges
    let chargesProperties hide =
        if hide then [|
            nameof asi.NewCharges
            nameof asi.ChargesPortion
            nameof asi.ChargesBalance
        |] else [||]
    /// an array of properties relating to quotes
    let quoteProperties hide =
        if hide then [|
            nameof asi.GeneratedPayment
        |] else [||]
    /// an array of properties representing extra information
    let extraProperties hide =
        if hide then [|
            nameof asi.FeesRefundIfSettled
            nameof asi.SettlementFigure
            nameof asi.Window
        |] else [||]
    /// an array of properties representing internal interest calculations
    let interestProperties hide =
        if hide then [|
            nameof asi.OriginalSimpleInterest
            nameof asi.ContractualInterest
            nameof asi.SimpleInterest
        |] else [||]

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
                go.GoParameters.FeeConfig.FeeTypes |> Array.isEmpty |> feesProperties
                go.GoParameters.ChargeConfig.ChargeTypes |> Array.isEmpty |> chargesProperties
                (match go.GoPurpose with IntendedPurpose.Settlement _ -> false | _ -> true) |> quoteProperties
                (match go.GoParameters.InterestConfig.Method with Interest.Method.AddOn -> false | _ -> true) |> interestProperties
                go.GoExtra |> not |> extraProperties
            |]
            |> Array.concat
        | None ->
            [||]
