namespace FSharp.Finance.Personal

open Calculation
open DateDay

/// a product fee
/// > NOTE: differences between fees and charges:
/// > - fees are up-front amounts paid under agreed terms for receiving an advance
/// > - fees are added to the principal balance and therefore accrue interest
module Fee =

    /// the type and amount of any fees, such as facilitation fees or CSO/CAB fees, taking into account any constraints
    [<Struct; StructuredFormatDisplay("{Html}")>]
    type FeeType =
        /// a fee enabling the provision of a financial product
        | FacilitationFee of FacilitationFee: Amount
        /// a fee charged by a Credit Access Business (CAB) or Credit Services Organisation (CSO) assisting access to third-party financial products
        | CabOrCsoFee of CabOrCsoFee: Amount
        /// a fee charged by a bank or building society for arranging a mortgage 
        | MortageFee of MortageFee: Amount
        /// any other type of product fee
        | CustomFee of FeeType: string * FeeAmount: Amount
        /// HTML formatting to display the fee type in a readable format
        member ft.Html =
            match ft with
            | FacilitationFee amount -> $"facilitation fee {amount}"
            | CabOrCsoFee amount -> $"CAB/CSO fee {amount}"
            | MortageFee amount -> $"mortgage fee {amount}"
            | CustomFee (name, amount) -> $"{name} {amount}"
            |> _.Replace(" ", "&nbsp;")

    /// how to amortise fees
    [<Struct; StructuredFormatDisplay("{Html}")>]
    type FeeAmortisation =
        /// amortise any fee before amortising principal
        | AmortiseBeforePrincipal
        /// amortise any fee and principal proportionately
        | AmortiseProportionately
        /// HTML formatting to display the fee amortisation in a readable format
        member fa.Html =
            match fa with
            | AmortiseBeforePrincipal -> "amortise before principal"
            | AmortiseProportionately -> "amortise proportionately"

    /// how the fees are treated in the event of an early settlement
    [<RequireQualifiedAccess; Struct; StructuredFormatDisplay("{Html}")>]
    type SettlementRefund =
        /// fees are due in full with no discount or refund
        | Zero
        /// for original (non-rescheduled) amortisations: fees are refunded proportionately based on the current final payment day
        | ProRata
        /// for rescheduled amortisations: fees are refunded proportionately based on the original final payment day
        | ProRataRescheduled of OriginalFinalPaymentDay: int<OffsetDay>
        /// the current fee balance is refunded
        | Balance
        /// HTML formatting to display the settlement refund in a readable format
        member sr.Html =
            match sr with
            | Zero -> "no refund"
            | ProRata -> "pro-rata refund"
            | ProRataRescheduled day -> $"pro rata refund (based on day {day})"
            | Balance -> "balance refund"

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
        Rounding: Rounding
        /// how to amortise fees in relation to principal
        FeeAmortisation: FeeAmortisation
        /// how fees are treated when a product is repaid early
        SettlementRefund: SettlementRefund
    }

    /// options specifying the types of fees, their amounts, and any restrictions on these
    module Config =
        /// a default config value, with no fees but recommended settings
        let initialRecommended = {
            FeeTypes = [||]
            Rounding = NoRounding
            FeeAmortisation = AmortiseProportionately
            SettlementRefund = SettlementRefund.ProRata
        }

        /// formats the fee config as an HTML table
        let toHtmlTable config =
            "<table>"
                + "<tr>"
                    + $"<td>fee types: <i>{Array.toStringOrNa config.FeeTypes}</i></td>"
                    + $"<td>rounding: <i>{config.Rounding}</i></td>"
                + "</tr>"
                + "<tr>"
                    + $"<td>fee amortisation: <i>{config.FeeAmortisation}</i></td>"
                    + $"<td>settlement refund: <i>{config.SettlementRefund}</i></td>"
                + "</tr>"
            + "</table>"

    /// rounds a charge to the nearest integer cent using the specified rounding option
    let round config =
        Cent.fromDecimalCent config.Rounding

    /// calculates the total amount of fees based on the fee configuration
    let total feeConfig baseValue feeType =
        match feeType with
        | FacilitationFee amount
        | CabOrCsoFee amount
        | MortageFee amount
        | CustomFee (_, amount) -> amount |> Amount.total baseValue
        |> round feeConfig

    /// calculates the total sum of all fees based on either default or custom fee types
    let grandTotal feeConfig baseValue customFeeTypes =
        customFeeTypes
        |> ValueOption.defaultValue feeConfig.FeeTypes
        |> Array.sumBy (total feeConfig baseValue)
