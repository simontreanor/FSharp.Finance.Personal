namespace FSharp.Finance.Personal

open Calculation
open DateDay
open Formatting

/// a product fee
/// > NOTE: differences between fee and charge:
/// > - a fee is an up-front amount paid under agreed terms for receiving an advance
/// > - a fee is added to the principal balance and therefore accrues interest
module Fee =

    /// the type and amount of any fee, such as facilitation fee or CSO/CAB fee, taking into account any constraints
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
            | FacilitationFee amount -> $"<i>facilitation fee</i> {amount}"
            | CabOrCsoFee amount -> $"<i>CAB/CSO fee</i> {amount}"
            | MortageFee amount -> $"<i>mortgage fee</i> {amount}"
            | CustomFee (name, amount) -> $"<i>{name}</i> {amount}"

    /// how to amortise the fee
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

    /// how the fee is treated in the event of an early settlement
    [<RequireQualifiedAccess; Struct; StructuredFormatDisplay("{Html}")>]
    type SettlementRefund =
        /// fee is due in full with no discount or refund
        | Zero
        /// for original (non-rescheduled) amortisations: fee is refunded proportionately based on the current final payment day
        | ProRata
        /// for rescheduled amortisations: fee is refunded proportionately based on the original final payment day
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

    /// how to handle any fee when rescheduling or rolling over
    [<Struct>]
    type FeeHandling =
        /// move any outstanding fee balance to the principal balance
        | CapitaliseAsPrincipal
        /// carry any outstanding fee balance over as an initial fee balance, maintaining the original final payment day if pro-rated
        | CarryOverAsIs
        /// write off any outstanding fee balance
        | WriteOffFeeBalance

    /// options specifying the type of fee and how it is calculated
    type Config = {
        /// a fee applicable to a product
        FeeType: FeeType
        /// how to round the fee
        Rounding: Rounding
        /// how to amortise the fee in relation to the principal
        FeeAmortisation: FeeAmortisation
        /// how the fee is treated when a product is repaid early
        SettlementRefund: SettlementRefund
    }

    /// options specifying the type of fee and how it is calculated
    module Config =
        /// formats the fee config as an HTML table
        let toHtmlTable config =
            match config with
            | Some c ->
                "<table>"
                    + "<tr>"
                        + $"""<td>fee type: <i>{c.FeeType}</i></td>"""
                        + $"<td>rounding: <i>{c.Rounding}</i></td>"
                    + "</tr>"
                    + "<tr>"
                        + $"<td>fee amortisation: <i>{c.FeeAmortisation}</i></td>"
                        + $"<td>settlement refund: <i>{c.SettlementRefund}</i></td>"
                    + "</tr>"
                + "</table>"
            | None ->
                "no fee"

    /// calculates the total fee based on the fee configuration
    let total feeConfig baseValue =
        match feeConfig with
        | Some fc ->
            match fc.FeeType with
            | FacilitationFee amount
            | CabOrCsoFee amount
            | MortageFee amount
            | CustomFee (_, amount) -> amount |> Amount.total baseValue
            |> Cent.fromDecimalCent fc.Rounding
        | None ->
            0L<Cent>
