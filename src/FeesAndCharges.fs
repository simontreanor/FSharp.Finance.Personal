namespace FSharp.Finance.Personal

open System

[<AutoOpen>]
module FeesAndCharges =
    
    /// the type and amount of any fees, such as facilitation fees or CSO/CAB fees, taking into account any constraints
    /// 
    /// NOTE: differences between fees and charges:
    /// - fees are up-front amounts paid under agreed terms for receiving an advance
    /// - fees are added to the principal balance and therefore accrue interest
    [<RequireQualifiedAccess>]
    [<Struct>]
    type Fees =
        /// a percentage of the principal, optionally capped to a fixed amount
        | Percentage of Percentage:Percent * Cap:int64<Cent> voption
        /// a fixed fee
        | Simple of Simple:int64<Cent>

    module Fees =
        /// calculates the total amound of fees based on the fee configuration
        let total (principal: int64<Cent>) fees =
            match fees with
            | ValueSome (Fees.Percentage (Percent percentage, ValueSome cap)) ->
                Decimal.Floor(decimal principal * decimal percentage) / 100m |> fun m -> (int64 m * 1L<Cent>)
                |> fun cents -> Cent.min cents cap
            | ValueSome (Fees.Percentage (Percent percentage, ValueNone)) ->
                Decimal.Floor(decimal principal * decimal percentage) / 100m |> fun m -> (int64 m * 1L<Cent>)
            | ValueSome (Fees.Simple simple) -> simple
            | ValueNone -> 0L<Cent>

        /// how the fees are treated in the event of an early settlement
        [<Struct>]
        type Settlement =
            /// the initial fees are due in full with no discount or refund
            | DueInFull
            /// the fees are refunded proportionately to the number of days elapsed compared to the original scheduled number of days
            | ProRataRefund

    /// the type and amount of charge
    /// 
    /// NOTE: differences between charges and fees:
    /// - charges are not up-front amounts, they are incurred as a result of a breach of agreed terms
    /// - charges are not added to the principal balance and do not therefore accrue interest
    [<RequireQualifiedAccess>]
    [<Struct>]
    type Charge =
        /// a charge incurred because a scheduled payment was not made on time or in full
        | LatePayment of LatePayment:int64<Cent>
        /// a charge incurred to cover banking costs when a payment attempt is rejected due to a lack of funds
        | InsufficientFunds of InsufficientFunds:int64<Cent>

    module Charges =
        /// calculates the total of any charges incurred
        let total charges =
            charges
            |> Array.sumBy(function Charge.LatePayment m | Charge.InsufficientFunds m -> m)

    [<Struct>]
    type FeesAndCharges = {
        Fees: Fees voption
        FeesSettlement: Fees.Settlement
        Charges: Charge array
    }

    module FeesAndCharges =
        /// recommended fees options
        let recommended = {
            Fees = ValueNone
            FeesSettlement = Fees.Settlement.ProRataRefund
            Charges = [||]
        }
