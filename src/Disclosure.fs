namespace FSharp.Finance.Personal

open System

/// Computing key regulatory metrics required for consumer credit documentation across UK, EU and US regimes.
module Disclosure =

    open Amortisation
    open Apr
    open Calculation
    open DateDay
    open Scheduling

    // --------------------------------------------------------
    // Private helpers
    // --------------------------------------------------------

    /// extracts advance transfers from an amortisation schedule
    let private advanceTransfers (items: Map<int<OffsetDay>, ScheduleItem>) =
        items
        |> Map.toArray
        |> Array.collect (fun (_, si) ->
            si.Advances
            |> Array.map (fun a -> {
                TransferType = Advance
                TransferDate = si.OffsetDate
                Value = a
            })
        )

    /// extracts scheduled payment transfers from an amortisation schedule
    let private scheduledPaymentTransfers (items: Map<int<OffsetDay>, ScheduleItem>) =
        items
        |> Map.toArray
        |> Array.choose (fun (_, si) ->
            let total = ScheduledPayment.total si.ScheduledPayment

            if total > 0L<Cent> then
                Some {
                    TransferType = Payment
                    TransferDate = si.OffsetDate
                    Value = total
                }
            else
                None
        )

    /// computes the total cost of credit from the amortisation schedule (sum of interest, fee and charges portions)
    let private totalCostOfCredit (items: Map<int<OffsetDay>, ScheduleItem>) =
        items
        |> Map.toArray
        |> Array.sumBy (fun (_, si) -> si.InterestPortion + si.FeePortion + si.ChargesPortion)

    /// computes the APR using the given method from the amortisation schedule
    let private computeApr (aprMethod: CalculationMethod) (items: Map<int<OffsetDay>, ScheduleItem>) =
        let advances = advanceTransfers items
        let payments = scheduledPaymentTransfers items
        let principal = advances |> Array.sumBy _.Value

        let advanceDate =
            if Array.isEmpty advances then
                Unchecked.defaultof<Date>
            else
                advances |> Array.map _.TransferDate |> Array.min

        Apr.calculate aprMethod principal advanceDate payments
        |> Apr.toPercent aprMethod

    // ============================================================
    // FCA MCOB / CONC (UK mortgages and consumer credit)
    // ============================================================

    /// key UK FCA CONC disclosure figures for consumer credit documentation
    [<Struct>]
    type UkConc = {
        /// the Annual Percentage Rate per FCA CONC App 1.2 rules
        Apr: Percent
        /// the total cost of credit (sum of interest, fees and charges)
        TotalCostOfCredit: int64<Cent>
        /// the total amount payable (principal plus total cost of credit)
        TotalAmountPayable: int64<Cent>
        /// the representative APR, equal to the contractual APR for a single-advance transaction
        RepresentativeApr: Percent
    }

    /// computes UK FCA CONC disclosure figures from a fully-populated amortisation schedule
    ///
    /// - APR is computed using the UK FCA method per CONC App 1.2
    /// - `aprPrecision` is the number of decimal places for the APR value as a raw decimal (note this is two more than the displayed percentage precision)
    let ukConc (aprPrecision: int) (amortisationSchedule: Schedule) =
        let items = amortisationSchedule.ScheduleItems
        let advances = advanceTransfers items
        let principal = advances |> Array.sumBy _.Value
        let tcc = totalCostOfCredit items
        let apr = computeApr (CalculationMethod.UnitedKingdom aprPrecision) items

        {
            Apr = apr
            TotalCostOfCredit = tcc
            TotalAmountPayable = principal + tcc
            RepresentativeApr = apr
        }

    // ============================================================
    // EU Consumer Credit Directive — SECCI
    // ============================================================

    /// key EU SECCI disclosure figures for the Standard European Consumer Credit Information sheet
    [<Struct>]
    type EuSecci = {
        /// the Annual Percentage Rate per EU Directive 2008/48/EC
        Apr: Percent
        /// the total cost of credit (sum of interest, fees and charges)
        TotalCostOfCredit: int64<Cent>
        /// the total amount payable (principal plus total cost of credit)
        TotalAmountPayable: int64<Cent>
    }

    /// the repayment schedule entry for the EU SECCI, showing the date and amount of each scheduled payment
    [<Struct>]
    type EuSecciRepaymentEntry = {
        /// the date on which the payment is due
        PaymentDate: Date
        /// the amount due on that date
        Amount: int64<Cent>
    }

    /// computes EU SECCI disclosure figures from a fully-populated amortisation schedule
    ///
    /// - APR is computed using the EU CCD formula per Directive 2008/48/EC
    /// - `aprPrecision` is the number of decimal places for the APR value as a raw decimal (note this is two more than the displayed percentage precision)
    let euSecci (aprPrecision: int) (amortisationSchedule: Schedule) =
        let items = amortisationSchedule.ScheduleItems
        let advances = advanceTransfers items
        let principal = advances |> Array.sumBy _.Value
        let tcc = totalCostOfCredit items
        let apr = computeApr (CalculationMethod.EuropeanUnion aprPrecision) items

        {
            Apr = apr
            TotalCostOfCredit = tcc
            TotalAmountPayable = principal + tcc
        }

    /// extracts the repayment schedule from a fully-populated amortisation schedule for inclusion in an EU SECCI sheet
    let euSecciRepaymentSchedule (amortisationSchedule: Schedule) =
        amortisationSchedule.ScheduleItems
        |> scheduledPaymentTransfers
        |> Array.map (fun t -> { PaymentDate = t.TransferDate; Amount = t.Value })

    // ============================================================
    // US TILA (Truth in Lending Act)
    // ============================================================

    /// key US TILA disclosure figures for Truth in Lending Act documentation
    [<Struct>]
    type UsTila = {
        /// the total finance charge (total cost of credit expressed in dollar terms)
        FinanceCharge: int64<Cent>
        /// the amount financed (principal minus any prepaid finance charges collected at origination)
        AmountFinanced: int64<Cent>
        /// the Annual Percentage Rate per TILA / Regulation Z (12 CFR Part 1026, Appendix J)
        Apr: Percent
    }

    /// computes US TILA disclosure figures from a fully-populated amortisation schedule
    ///
    /// - APR is computed using the US CFPB actuarial method per 12 CFR Part 1026 Appendix J
    /// - `aprPrecision` is the number of decimal places for the APR value as a raw decimal (note this is two more than the displayed percentage precision)
    let usTila (aprPrecision: int) (amortisationSchedule: Schedule) =
        let items = amortisationSchedule.ScheduleItems
        let advances = advanceTransfers items
        let principal = advances |> Array.sumBy _.Value

        // prepaid finance charges are any fees collected at origination (offset day 0)
        let prepaidFinanceCharges =
            items
            |> Map.tryFind 0<OffsetDay>
            |> Option.map _.FeePortion
            |> Option.defaultValue 0L<Cent>

        let financeCharge = totalCostOfCredit items
        let apr = computeApr (CalculationMethod.UsActuarial aprPrecision) items

        {
            FinanceCharge = financeCharge
            AmountFinanced = principal - prepaidFinanceCharges
            Apr = apr
        }
