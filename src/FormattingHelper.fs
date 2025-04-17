namespace FSharp.Finance.Personal

/// convenience module for generating HTML tables, optimised for amortisation schedules
module FormattingHelper =

    open Calculation

    let private asi = Unchecked.defaultof<Amortisation.ScheduleItem>
 
    /// an array of properties relating to (product) fee
    let hideFeeProperties =
        [|
            nameof asi.FeePortion
            nameof asi.FeeRebate
            nameof asi.FeeBalance
            nameof asi.FeeRebateIfSettled
        |]
    /// an array of properties relating to (penalty) charges
    let hideChargesProperties =
        [|
            nameof asi.NewCharges
            nameof asi.ChargesPortion
            nameof asi.ChargesBalance
        |]
    /// an array of properties relating to quotes
    let hideQuoteProperties =
        [|
            nameof asi.GeneratedPayment
        |]
    /// an array of properties representing extra information
    let hideExtraProperties =
        [|
            nameof asi.FeeRebateIfSettled
            nameof asi.SettlementFigure
            nameof asi.Window
        |]
    /// an array of properties representing internal interest calculations
    let hideInterestProperties =
        [|
            nameof asi.SimpleInterest
        |]

    /// a set of options specifying which fields to show/hide in the output
    type GenerationOptions = {
        GoParameters: Scheduling.Parameters
        GoQuote: bool
        GoExtra: bool
    }

    /// determines which fields to hide
    let getHideProperties generationOptions =
        match generationOptions with
        | Some go ->
            [|
                if go.GoParameters.FeeConfig.IsNone then hideFeeProperties else [||]
                if go.GoParameters.ChargeConfig.IsNone || Map.isEmpty go.GoParameters.ChargeConfig.Value.ChargeTypes then hideChargesProperties else [||]
                if (match go.GoParameters.InterestConfig.Method with Interest.Method.AddOn -> false | _ -> true) then hideInterestProperties else [||]
                if not go.GoQuote then hideQuoteProperties else [||]
                if not go.GoExtra then hideExtraProperties else [||]
            |]
            |> Array.concat
        | None ->
            [||]
