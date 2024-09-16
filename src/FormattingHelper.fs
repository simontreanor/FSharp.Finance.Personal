namespace FSharp.Finance.Personal

/// convenience module for generating HTML tables, optimised for amortisation schedules
module FormattingHelper =

    open Calculation

    /// an array of properties relating to (product) fees
    let feesProperties hide =
        if hide then [|
            nameof Unchecked.defaultof<Amortisation.ScheduleItem>.FeesBalance
            nameof Unchecked.defaultof<Amortisation.ScheduleItem>.FeesPortion
            nameof Unchecked.defaultof<Amortisation.ScheduleItem>.FeesRefund
            nameof Unchecked.defaultof<Amortisation.ScheduleItem>.FeesRefundIfSettled
            nameof Unchecked.defaultof<Amortisation.ScheduleItem>.FeesToDate
        |] else [||]
    /// an array of properties relating to (penalty) charges
    let chargesProperties hide =
        if hide then [|
            nameof Unchecked.defaultof<Amortisation.ScheduleItem>.ChargesBalance
            nameof Unchecked.defaultof<Amortisation.ScheduleItem>.ChargesPortion
            nameof Unchecked.defaultof<Amortisation.ScheduleItem>.ChargesToDate
            nameof Unchecked.defaultof<Amortisation.ScheduleItem>.NewCharges
        |] else [||]
    /// an array of properties relating to quotes
    let quoteProperties hide =
        if hide then [|
            nameof Unchecked.defaultof<Amortisation.ScheduleItem>.GeneratedPayment
        |] else [||]
    /// an array of properties representing extra information
    let extraProperties hide =
        if hide then [|
            nameof Unchecked.defaultof<Amortisation.ScheduleItem>.ChargesToDate
            nameof Unchecked.defaultof<Amortisation.ScheduleItem>.InterestToDate
            nameof Unchecked.defaultof<Amortisation.ScheduleItem>.FeesToDate
            nameof Unchecked.defaultof<Amortisation.ScheduleItem>.PrincipalToDate
            nameof Unchecked.defaultof<Amortisation.ScheduleItem>.FeesRefundIfSettled
            nameof Unchecked.defaultof<Amortisation.ScheduleItem>.SettlementFigure
            nameof Unchecked.defaultof<Amortisation.ScheduleItem>.Window
        |] else [||]
    /// an array of properties representing internal interest calculations
    let interestProperties hide =
        if hide then [|
            nameof Unchecked.defaultof<Amortisation.ScheduleItem>.ContractualInterest
            nameof Unchecked.defaultof<Amortisation.ScheduleItem>.OriginalSimpleInterest
            nameof Unchecked.defaultof<Amortisation.ScheduleItem>.OriginalSimpleInterestToDate
            nameof Unchecked.defaultof<Amortisation.ScheduleItem>.SimpleInterest
            nameof Unchecked.defaultof<Amortisation.ScheduleItem>.SimpleInterestToDate
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
                go.GoParameters.FeesAndCharges.Fees |> Array.isEmpty |> feesProperties
                go.GoParameters.FeesAndCharges.Charges |> Array.isEmpty |> chargesProperties
                (match go.GoPurpose with IntendedPurpose.Settlement _ -> false | _ -> true) |> quoteProperties
                (match go.GoParameters.Interest.Method with Interest.Method.AddOn -> false | _ -> true) |> interestProperties
                go.GoExtra |> not |> extraProperties
            |]
            |> Array.concat
        | None ->
            [||]
