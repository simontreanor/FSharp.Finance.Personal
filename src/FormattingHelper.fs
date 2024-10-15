namespace FSharp.Finance.Personal

/// convenience module for generating HTML tables, optimised for amortisation schedules
module FormattingHelper =

    open Calculation

    let private asi = Unchecked.defaultof<Amortisation.ScheduleItem>
 
    /// an array of properties relating to (product) fees
    let hideFeesProperties =
        [|
            nameof asi.FeesPortion
            nameof asi.FeesRefund
            nameof asi.FeesBalance
            nameof asi.FeesRefundIfSettled
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
            nameof asi.FeesRefundIfSettled
            nameof asi.SettlementFigure
            nameof asi.Window
        |]
    /// an array of properties representing internal interest calculations
    let hideInterestProperties =
        [|
            nameof asi.OriginalSimpleInterest
            nameof asi.ContractualInterest
            nameof asi.SimpleInterest
        |]

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
                if Array.isEmpty go.GoParameters.FeeConfig.FeeTypes then hideFeesProperties else [||]
                if Array.isEmpty go.GoParameters.ChargeConfig.ChargeTypes then hideChargesProperties else [||]
                if (match go.GoPurpose with IntendedPurpose.SettlementOn _ | IntendedPurpose.SettlementOnAsOfDay -> false | _ -> true) then hideQuoteProperties else [||]
                if (match go.GoParameters.InterestConfig.Method with Interest.Method.AddOn -> false | _ -> true) then hideInterestProperties else [||]
                if not go.GoExtra then hideExtraProperties else [||]
            |]
            |> Array.concat
        | None ->
            [||]
