namespace FSharp.Finance.Personal

open System

/// categorising (penalty) charges and (product) fees, their types and restrictions
[<AutoOpen>]
module FeesAndCharges =

    /// the type of restriction placed on a possible value
    [<Struct>]
    type Restriction =
        /// prevent values below a certain limit
        | LowerLimit of LowerLimit:int64<Cent>
        /// prevent values above a certain limit
        | UpperLimit of UpperLimit:int64<Cent>
        /// constrain values to within a range
        | WithinRange of MinValue:int64<Cent> * MaxValue:int64<Cent>

    /// either a simple amount or an amount constrained by limits
    [<RequireQualifiedAccess>]
    module Restriction =
        /// calculate a permitted value based on a restriction
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

    /// convenience functions for handling amounts
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
        /// a fee enabling the provision of a financial product
        | FacilitationFee of FacilitationFee:Amount
        /// a fee charged by a Credit Access Business (CAB) or Credit Services Organisation (CSO) assisting access to third-party financial products
        | CabOrCsoFee of CabOrCsoFee:Amount
        /// any other type of product fee
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
            /// the fees are refunded proportionately to the number of days elapsed in the current schedule
            | ProRataRefund

        /// how to handle fees when rescheduling or rolling over
        [<Struct>]
        type FeeHandling =
            /// move any outstanding fee balance to the principal balance
            | CapitaliseAsPrincipal
            /// carry any outstanding fee balance over as an initial fee balance, maintaining the original final payment day if pro-rated
            | CarryOverAsIs
            /// write off any outstanding fee balance
            | WriteOffFeeBalance

    /// the type and amount of any charge
    [<RequireQualifiedAccess; Struct>]
    type Charge =
        /// a charge incurred because a scheduled payment was not made on time or in full
        | LatePayment of LatePayment:Amount
        /// a charge incurred to cover banking costs when a payment attempt is rejected due to a lack of funds
        | InsufficientFunds of InsufficientFunds:Amount
        /// any other type of penalty charge
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

        /// determines whether charge are applicable on a given day
        let areApplicable (startDate: Date) holidays (onDay: int<OffsetDay>) =
            holidays
            |> Array.collect(fun (ih: Holiday) ->
                [| (ih.Start - startDate).Days .. (ih.End - startDate).Days |]
            )
            |> Array.exists(fun d -> d = int onDay)
            |> not

    /// options on how to handle multiple charges
    [<Struct>]
    type ChargesGrouping =
        /// only one charge of any type may be applied per day
        | OneChargeTypePerDay
        /// all charges are applied
        | AllChargesApplied

    /// options specifying the types of fees and charges, their amounts, and any restrictions on these
    [<Struct>]
    type FeesAndCharges = {
        /// a list of product fees applicable to a product
        Fees: Fee array
        /// how fees are treated when a product is repaid early
        FeesSettlement: Fees.Settlement
        /// a list of penalty charges applicable to a product
        Charges: Charge array
        /// any period during which charges are not payable
        ChargesHolidays: Holiday array
        /// whether to group charges by type per day
        ChargesGrouping: ChargesGrouping
        /// the number of days' grace period after which late-payment charges apply
        LatePaymentGracePeriod: int<DurationDay>
    }
