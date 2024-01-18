namespace FSharp.Finance.Personal

open System

[<AutoOpen>]
module FeesAndCharges =

    [<Struct>]
    type Restriction =
        | LowerLimit of LowerLimit:int64<Cent>
        | UpperLimit of UpperLimit:int64<Cent>
        | WithinRange of MinValue:int64<Cent> * MaxValue:int64<Cent>

    [<RequireQualifiedAccess>]
    module Restriction =
        let calculate restriction amount =
            match restriction with
            | ValueSome (LowerLimit a) -> amount |> Cent.max a
            | ValueSome (UpperLimit a) -> amount |> Cent.min a
            | ValueSome (WithinRange (min, max)) -> amount |> Cent.min max |> Cent.max min
            | ValueNone -> amount

    /// an amount specified either as a simple amount or as a percentage of another amount, optionally restricted to lower and/or upper limits
    [<RequireQualifiedAccess; Struct>]
    type Amount =
        /// a fixed fee
        | Simple of Simple:int64<Cent>
        /// a percentage of the principal, optionally restricted
        | Percentage of Percentage:Percent * Restriction:Restriction voption

    [<RequireQualifiedAccess>]
    module Amount =
        /// calculates the total amount based on any restrictions
        let total (baseAmount: int64<Cent>) amount =
            match amount with
            | Amount.Percentage (Percent percentage, restriction) ->
                Decimal.Floor(decimal baseAmount * decimal percentage) / 100m |> fun m -> (int64 m * 1L<Cent>)
                |> Restriction.calculate restriction
            | Amount.Simple simple -> simple


    /// the type and amount of any fees, such as facilitation fees or CSO/CAB fees, taking into account any constraints
    [<RequireQualifiedAccess; Struct>]
    type Fee =
        | FacilitationFee of FacilitationFee:Amount
        | CabOrCsoFee of CabOrCsoFee:Amount
        | CustomFee of FeeType:string * FeeAmount:Amount

    /// NOTE: differences between fees and charges:
    /// - fees are up-front amounts paid under agreed terms for receiving an advance
    /// - fees are added to the principal balance and therefore accrue interest
    module Fees =
        /// calculates the total amound of fees based on the fee configuration
        let total baseAmount fees =
            fees
            |> Array.sumBy(function
                | Fee.FacilitationFee amount
                | Fee.CabOrCsoFee amount
                | Fee.CustomFee (_, amount) -> amount |> Amount.total baseAmount
            )

        /// how the fees are treated in the event of an early settlement
        [<Struct>]
        type Settlement =
            /// the initial fees are due in full with no discount or refund
            | DueInFull
            /// the fees are refunded proportionately to the number of days elapsed compared to the original scheduled number of days
            | ProRataRefund

    /// the type and amount of any charge
    [<RequireQualifiedAccess; Struct>]
    type Charge =
        /// a charge incurred because a scheduled payment was not made on time or in full
        | LatePayment of LatePayment:Amount
        /// a charge incurred to cover banking costs when a payment attempt is rejected due to a lack of funds
        | InsufficientFunds of InsufficientFunds:Amount
        | CustomCharge of ChargeType:string * FeeAmount:Amount

    /// NOTE: differences between charges and fees:
    /// - charges are not up-front amounts, they are incurred as a result of a breach of agreed terms
    /// - charges are not added to the principal balance and do not therefore accrue interest
    module Charges =
        /// calculates the total of any charges incurred
        let total baseAmount charges =
            charges
            |> Array.sumBy(function
                | Charge.LatePayment amount
                | Charge.InsufficientFunds amount
                | Charge.CustomCharge (_, amount) -> amount |> Amount.total baseAmount
            )

    [<Struct>]
    type FeesAndCharges = {
        Fees: Fee array
        FeesSettlement: Fees.Settlement
        Charges: Charge array
        LatePaymentGracePeriod: int<DurationDay>
    }

    module FeesAndCharges =
        /// recommended fees options
        let recommended = {
            Fees = [||]
            FeesSettlement = Fees.Settlement.ProRataRefund
            Charges = [||]
            LatePaymentGracePeriod = 3<DurationDay>
        }
