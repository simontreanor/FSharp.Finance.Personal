namespace FSharp.Finance.Personal

open Calculation
open DateDay

/// a penalty charge
/// > NB: differences between charges and fees:
/// > - charges are not up-front amounts, they are incurred as a result of a breach of agreed terms
/// > - charges are not added to the principal balance and do not therefore accrue interest
module Charge =

    /// the type and amount of any charge
    [<Struct>]
    type ChargeType =
        /// a charge incurred because a scheduled payment was not made on time or in full
        | LatePayment of LatePayment: Amount
        /// a charge incurred to cover banking costs when a payment attempt is rejected due to a lack of funds
        | InsufficientFunds of InsufficientFunds: Amount
        /// any other type of penalty charge
        | CustomCharge of ChargeType: string * FeeAmount: Amount

    /// options on how to handle multiple charges
    [<Struct>]
    type ChargeGrouping =
        /// only one charge of any type may be applied per day
        | OneChargeTypePerDay
        /// only one charge of any type may be applied per product
        | OneChargeTypePerProduct
        /// all charges are applied
        | AllChargesApplied

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
        /// a default config value, with no charges but recommended settings
        let initialRecommended = {
            ChargeTypes = [||]
            Rounding = NoRounding
            ChargeHolidays = [||]
            ChargeGrouping = OneChargeTypePerDay
            LatePaymentGracePeriod = 0<DurationDay>
        }
        /// formats the charge config as an HTML table
        let toHtmlTable config =
            "<table>" +
                $"<tr><td>{nameof config.ChargeTypes}</td><td><table>" +
                    (config.ChargeTypes |> Array.map (fun ct -> $"<tr><td>{ct}</td></tr>") |> String.concat "")
                + "</table></td></tr>" +
                $"<tr><td>{nameof config.Rounding}</td><td>{config.Rounding}</td></tr>" +
                $"<tr><td>{nameof config.ChargeHolidays}</td><td><table>" +
                    (config.ChargeHolidays |> Array.map (fun ch -> $"<tr><td>{ch}</td></tr>") |> String.concat "")
                + "</table></td></tr>" +
                $"<tr><td>{nameof config.ChargeGrouping}</td><td>{config.ChargeGrouping}</td></tr>" +
                $"<tr><td>{nameof config.LatePaymentGracePeriod}</td><td>{config.LatePaymentGracePeriod}</td></tr>"
            + "</table>"

    /// rounds a charge to the nearest integer cent using the specified rounding option
    let round config =
        Cent.fromDecimalCent config.Rounding

    /// calculates the total of a charge
    let total chargeConfig baseValue chargeType =
        match chargeType with
        | LatePayment amount
        | InsufficientFunds amount
        | CustomCharge (_, amount) -> amount |> Amount.total baseValue
        |> round chargeConfig

    /// calculates the total sum of any charges incurred
    let grandTotal chargeConfig baseValue customChargeTypes =
        customChargeTypes
        |> ValueOption.defaultValue chargeConfig.ChargeTypes
        |> Array.sumBy (total chargeConfig baseValue)

    /// generates a date range during which charges are not incurred
    let holidayDates chargeConfig startDate =
        chargeConfig.ChargeHolidays
        |> Array.collect(fun (ih: DateRange) ->
            [| (ih.Start - startDate).Days .. (ih.End - startDate).Days |]
        )
