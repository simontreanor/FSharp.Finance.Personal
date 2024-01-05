namespace FSharp.Finance.Personal

open System
open ActualPayment

/// functions for rescheduling payments after an original schedule failed to amortise
module Rescheduling =

    /// based on a fixed payment amount, calculate the additional payments to a schedule required to fully amortise the transaction
    let reschedule (paymentAmount: int64<Cent>) (sp: ScheduledPayment.ScheduleParameters) scheduledPayments actualPayments (amortisationSchedule: AmortisationScheduleItem array) =
        let asOfDay = sp.AsOfDate |> OffsetDay.fromDate sp.StartDate
        let finalAppliedPayment = amortisationSchedule |> Array.last
        let oldFinalPaymentDay = finalAppliedPayment.OffsetDay
        let outstandingBalance = finalAppliedPayment |> fun p -> p.PrincipalBalance + p.FeesBalance + p.InterestBalance + p.ChargesBalance
        let iterationsToPayOffExistingBalance = decimal outstandingBalance / decimal paymentAmount |> Math.Ceiling
        let unitPeriodLenth = sp.UnitPeriodConfig |> UnitPeriod.Config.roughLength
        let estimatedYears = unitPeriodLenth * iterationsToPayOffExistingBalance / 365m
        let annualInterestRate = sp.Interest.Rate |> Interest.Rate.annual |> Percent.toDecimal
        let totalIterations = (1m + (annualInterestRate * estimatedYears)) * iterationsToPayOffExistingBalance |> Math.Ceiling
        let limit = int finalAppliedPayment.OffsetDay + int (totalIterations * unitPeriodLenth)
        let rescheduledPaymentStartDay = 177<OffsetDay>
        let extraScheduledPayments =
            [| int rescheduledPaymentStartDay .. int unitPeriodLenth .. limit |]
            |> Array.map(fun d ->
                ({ PaymentDay = d * 1<OffsetDay>; PaymentDetails = ScheduledPayment 2000L<Cent> } : ActualPayment.Payment)
            )
        let actualPayments' = Array.concat [| actualPayments; extraScheduledPayments |]
        scheduledPayments
        |> ActualPayment.applyPayments asOfDay 1000L<Cent> actualPayments'
        |> ActualPayment.calculateSchedule sp ValueNone oldFinalPaymentDay
