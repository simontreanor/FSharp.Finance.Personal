namespace FSharp.Finance

module AmortisationSchedule

type AmortisationSchedule = {
    LoanCalculationParameters: LoanCalculationParameters
    LoanCalculationResults: LoanCalculationResults
    Items: List<AmortisationScheduleItem>
}
member x.APR =
    (x.LoanCalculationParameters, (x.Items |> List.map(fun asi -> asi.Date, asi.Repayment * 1m<GBP>)))
    ||> fun lcp payments -> ``APR US Calculation`` lcp.RepaymentFrequency lcp.StartDate (lcp.Principal * 1m<GBP>) (payments |> List.toArray) ""
member x.CabFeeRefund = x.Items |> List.sumBy(fun asi -> asi.CabFeeRefund)
member x.EffectiveAnnualInterestRate = (x.TotalInterest / (x.TotalPrincipal + x.TotalCabFee - x.CabFeeRefund)) * (365m / x.LoanTermDays) //todo: verify this (used to check that interest does not exceed legal limit when repaying early)
member x.EffectiveDailyInterestRate = (x.TotalInterest / (x.TotalPrincipal + x.TotalCabFee - x.CabFeeRefund)) * (1m / x.LoanTermDays) //todo: verify this
member x.FinalRepaymentDate = x.Items |> List.maxBy(fun asi -> asi.Date) |> fun asi -> asi.Date
member x.LoanTermDays = x.Items |> List.maxBy(fun asi -> asi.Day) |> fun asi -> asi.Day
member x.RepaymentCount = x.Items |> List.length
member x.TotalCabFee = x.Items |> List.sumBy(fun asi -> asi.CabFeePortion)
member x.TotalInterest = x.Items |> List.sumBy(fun asi -> asi.InterestPortion)
member x.TotalNsfFee = x.Items |> List.sumBy(fun asi -> asi.NsfFeePortion)
member x.TotalPrincipal = x.Items |> List.sumBy(fun asi -> asi.PrincipalPortion)
member x.TotalRepayable = x.Items |> List.sumBy(fun asi -> asi.Repayment)
