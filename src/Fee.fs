namespace FSharp.Finance.Personal

open Amount
open Calculation
open Currency
open DateDay

/// a product fee
/// > NOTE: differences between fees and charges:
/// > - fees are up-front amounts paid under agreed terms for receiving an advance
/// > - fees are added to the principal balance and therefore accrue interest
module Fee =

    /// the type and amount of any fees, such as facilitation fees or CSO/CAB fees, taking into account any constraints
    [<Struct>]
    type FeeType =
        /// a fee enabling the provision of a financial product
        | FacilitationFee of FacilitationFee: Amount
        /// a fee charged by a Credit Access Business (CAB) or Credit Services Organisation (CSO) assisting access to third-party financial products
        | CabOrCsoFee of CabOrCsoFee: Amount
        /// a fee charged by a bank or building society for arranging a mortgage 
        | MortageFee of MortageFee: Amount
        /// any other type of product fee
        | CustomFee of FeeType: string * FeeAmount: Amount

    /// how to amortise fees
    [<Struct>]
    type FeeAmortisation =
        /// amortise any fee before amortising principal
        | AmortiseBeforePrincipal
        /// amortise any fee and principal proportionately
        | AmortiseProportionately

    /// how the fees are treated in the event of an early settlement
    [<RequireQualifiedAccess; Struct>]
    type SettlementRefund =
        /// fees are due in full with no discount or refund
        | None
        /// fees are refunded proportionately to the number of days elapsed in the current schedule, based on the original final payment day
        | ProRata of OriginalFinalPaymentDay: int<OffsetDay> voption
        /// the current fee balance is refunded
        | Balance

    /// how to handle fees when rescheduling or rolling over
    [<Struct>]
    type FeeHandling =
        /// move any outstanding fee balance to the principal balance
        | CapitaliseAsPrincipal
        /// carry any outstanding fee balance over as an initial fee balance, maintaining the original final payment day if pro-rated
        | CarryOverAsIs
        /// write off any outstanding fee balance
        | WriteOffFeeBalance

    /// options specifying the types of fees, their amounts, and any restrictions on these
    type Config = {
        /// a list of product fees applicable to a product
        FeeTypes: FeeType array
        /// how to round fees
        Rounding: Rounding voption
        /// how to amortise fees in relation to principal
        FeeAmortisation: FeeAmortisation
        /// how fees are treated when a product is repaid early
        SettlementRefund: SettlementRefund
    }

    module Config =
        /// a default config value, with no fees but recommended settings
        let DefaultValue = {
            FeeTypes = [||]
            Rounding = ValueNone
            FeeAmortisation = AmortiseProportionately
            SettlementRefund = SettlementRefund.ProRata ValueNone
        }

    /// rounds a charge to the nearest integer cent using the specified rounding option
    let Round config =
        Cent.fromDecimalCent config.Rounding

    /// calculates the total amount of fees based on the fee configuration
    let Total feeConfig baseAmount feeType =
        match feeType with
        | FacilitationFee amount
        | CabOrCsoFee amount
        | MortageFee amount
        | CustomFee (_, amount) -> amount |> Amount.total baseAmount
        |> Round feeConfig

    /// calculates the total sum of all fees based on either default or custom fee types
    let GrandTotal feeConfig baseAmount customFeeTypes =
        customFeeTypes
        |> ValueOption.defaultValue feeConfig.FeeTypes
        |> Array.sumBy (Total feeConfig baseAmount)
