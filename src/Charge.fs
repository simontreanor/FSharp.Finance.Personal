namespace FSharp.Finance.Personal

open Util
open Calculation
open Currency
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

    module Config =
        /// a default config value, with no charges but recommended settings
        let DefaultValue = {
            ChargeTypes = [||]
            Rounding = NoRounding
            ChargeHolidays = [||]
            ChargeGrouping = OneChargeTypePerDay
            LatePaymentGracePeriod = 0<DurationDay>
        }

    /// rounds a charge to the nearest integer cent using the specified rounding option
    let Round config =
        Cent.fromDecimalCent config.Rounding

    /// calculates the total of a charge
    let Total chargeConfig baseValue chargeType =
        match chargeType with
        | LatePayment amount
        | InsufficientFunds amount
        | CustomCharge (_, amount) -> amount |> Amount.total baseValue
        |> Round chargeConfig

    /// calculates the total sum of any charges incurred
    let GrandTotal chargeConfig baseValue customChargeTypes =
        customChargeTypes
        |> ValueOption.defaultValue chargeConfig.ChargeTypes
        |> Array.sumBy (Total chargeConfig baseValue)

    let HolidayDates chargeConfig startDate =
        chargeConfig.ChargeHolidays
        |> Array.collect(fun (ih: DateRange) ->
            [| (ih.Start - startDate).Days .. (ih.End - startDate).Days |]
        )
