namespace FSharp.Finance.Personal

open Calculation
open DateDay

/// a penalty charge
/// > NB: differences between charges and fees:
/// > - charges are not up-front amounts, they are incurred as a result of a breach of agreed terms
/// > - charges are not added to the principal balance and do not therefore accrue interest
module Charge =

    /// the type and amount of any charge
    [<Struct; StructuredFormatDisplay("{Html}")>]
    type ChargeType =
        /// a charge incurred because a scheduled payment was not made on time or in full
        | LatePayment of LatePayment: Amount
        /// a charge incurred to cover banking costs when a payment attempt is rejected due to a lack of funds
        | InsufficientFunds of InsufficientFunds: Amount
        /// any other type of penalty charge
        | CustomCharge of ChargeType: string * FeeAmount: Amount
        /// HTML formatting to display the charge type in a readable format
        member ct.Html =
            match ct with
            | LatePayment amount -> $"late payment {amount}"
            | InsufficientFunds amount -> $"insufficient funds {amount}"
            | CustomCharge (name, amount) -> $"{name} {amount}"

    /// options on how to handle multiple charges
    [<Struct; StructuredFormatDisplay("{Html}")>]
    type ChargeGrouping =
        /// only one charge of any type may be applied per day
        | OneChargeTypePerDay
        /// only one charge of any type may be applied per product
        | OneChargeTypePerProduct
        /// all charges are applied
        | AllChargesApplied
        /// HTML formatting to display the charge grouping in a readable format
        member cg.Html =
            match cg with
            | OneChargeTypePerDay -> "one charge per day"
            | OneChargeTypePerProduct -> "one charge per product"
            | AllChargesApplied -> "all charges applied"

    /// options specifying the types of charges, their amounts, and any restrictions on these
    type Config = {
        /// a list of penalty charges applicable to a product
        ChargeTypes: ChargeType array
        /// how to round charges
        Rounding: Rounding
        /// any period during which charges are not payable
        ChargeHolidays: DateRange array
        /// whether to group charges by type per day
        ChargeGrouping: ChargeGrouping
        /// the number of days' grace period after which late-payment charges apply
        LatePaymentGracePeriod: int<DurationDay>
    }

    /// options specifying the types of charges, their amounts, and any restrictions on these
    module Config =
        /// formats the charge config as an HTML table
        let toHtmlTable config =
            match config with
            | Some c ->
                "<table>"
                    + "<tr>"
                        + $"<td>charge types: <i>{Array.toStringOrNa c.ChargeTypes}</i></td>"
                        + $"<td>grouping: <i>{c.ChargeGrouping}</i></td>"
                    + "</tr>"
                    + $"<tr>"
                        + $"<td>rounding: <i>{c.Rounding}</i></td>"
                        + $"<td>late-payment grace period: <i>{c.LatePaymentGracePeriod}</i></td>"
                    + "</tr>"
                    + $"<tr>"
                        + $"<td colspan='2'>holidays: <i>{Array.toStringOrNa c.ChargeHolidays}</i></td>"
                    + "</tr>"
                + "</table>"
            | None ->
                "<table><tr><td>no charges</td></tr></table>"

    /// calculates the total of a charge
    let total chargeConfig baseValue chargeType =
        match chargeConfig with
        | Some cc ->
            match chargeType with
            | LatePayment amount
            | InsufficientFunds amount
            | CustomCharge (_, amount) -> amount |> Amount.total baseValue
            |> Cent.fromDecimalCent cc.Rounding
        | None ->
            0L<Cent>

    /// calculates the total sum of any charges incurred
    let grandTotal chargeConfig baseValue customChargeTypes =
        match chargeConfig with
        | Some cc ->
            customChargeTypes
            |> ValueOption.defaultValue cc.ChargeTypes
            |> Array.sumBy (total chargeConfig baseValue)
        | None ->
            0L<Cent>

    /// generates a date range during which charges are not incurred
    let holidayDates chargeConfig startDate =
        match chargeConfig with
        | Some cc ->
            cc.ChargeHolidays
            |> Array.collect(fun (dr: DateRange) ->
                [| (dr.Start - startDate).Days .. (dr.End - startDate).Days |]
            )
        | None ->
            [||]
