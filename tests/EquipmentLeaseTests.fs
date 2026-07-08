namespace FSharp.Finance.Personal.Tests

open Xunit
open FsUnit.Xunit
open FSharp.Finance.Personal.Calculation
open FSharp.Finance.Personal.EquipmentFinance
open FSharp.Finance.Personal.EquipmentFinance.Lease

module EquipmentLeaseTests =

    [<Fact>]
    let ``Payment frequency to payments per year conversion works`` () =
        Lease.PaymentFrequency.Monthly.PaymentsPerYear |> should equal 12
        Lease.PaymentFrequency.Quarterly.PaymentsPerYear |> should equal 4
        Lease.PaymentFrequency.SemiAnnual.PaymentsPerYear |> should equal 2
        Lease.PaymentFrequency.Annual.PaymentsPerYear |> should equal 1

    [<Fact>]
    let ``Lease payment calculation works for zero interest`` () =
        let terms = {
            EquipmentDescription = "Test equipment"
            FairMarketValue = 1200_00L<Cent> // $1,200
            TermMonths = 12
            LeaseType = Lease.LeaseType.OperatingLease
            PaymentFrequency = Lease.PaymentFrequency.Monthly
            PaymentTiming = Lease.PaymentTiming.InArrears
            LeasePayment = 0L<Cent>
            PeriodicFee = None
            UpfrontPayment = 0L<Cent>
            ResidualValue = 200_00L<Cent> // $200
            PurchaseOption = None
            ImplicitRate = FSharp.Finance.Personal.Interest.Rate.Zero
        }

        let payment = Lease.calculateLeasePayment terms
        // ($1200 - $200) / 12 = $83.33
        payment |> should equal 83_33L<Cent>

    [<Fact>]
    let ``Lease payment calculation works with interest`` () =
        let terms = {
            EquipmentDescription = "Manufacturing equipment"
            FairMarketValue = 10000_00L<Cent>
            TermMonths = 36
            LeaseType = Lease.LeaseType.FinanceLease
            PaymentFrequency = Lease.PaymentFrequency.Monthly
            PaymentTiming = Lease.PaymentTiming.InArrears
            LeasePayment = 0L<Cent>
            PeriodicFee = None
            UpfrontPayment = 1000_00L<Cent>
            ResidualValue = 2000_00L<Cent>
            PurchaseOption = Some 2000_00L<Cent>
            ImplicitRate = FSharp.Finance.Personal.Interest.Rate.Annual (FSharp.Finance.Personal.Calculation.Percent 5.0m)
        }

        let payment = Lease.calculateLeasePayment terms

        // Correct zero-interest baseline (uses upfront + residual)
        let zeroInterestBaseline =
            let initial = (terms.FairMarketValue - terms.UpfrontPayment - terms.ResidualValue) |> Cent.toDecimal
            initial / (decimal (terms.TermMonths * terms.PaymentFrequency.PaymentsPerYear / 12))
            |> Cent.fromDecimal

        payment |> should be (greaterThan zeroInterestBaseline)

        // Expected theoretical payment (recalculate in test for robustness)
        let r = 0.05m / 12m
        let n = 36
        let principalNet = Cent.toDecimal terms.FairMarketValue - Cent.toDecimal terms.UpfrontPayment
        let residual = Cent.toDecimal terms.ResidualValue
        let growth = System.Math.Pow(float (1m + r), float n) |> decimal
        let pvResidual = residual / growth
        let baseAmt = principalNet - pvResidual
        let annuityFactor = (1m - 1m / growth) / r
        let expectedDec = baseAmt / annuityFactor
        let expectedCents = Cent.fromDecimal expectedDec
        // Allow 1 cent tolerance for rounding differences
        abs (payment - expectedCents) |> should be (lessThanOrEqualTo 1L<Cent>)

    [<Fact>]
    let ``Lease details calculation includes total cost`` () =
        let terms = {
            EquipmentDescription = "Office equipment"
            FairMarketValue = 500000L<Cent> // $5,000
            TermMonths = 24
            LeaseType = Lease.LeaseType.OperatingLease
            PaymentFrequency = Lease.PaymentFrequency.Monthly
            PaymentTiming = Lease.PaymentTiming.InArrears
            LeasePayment = 200_00L<Cent> // $200/month
            PeriodicFee = None
            UpfrontPayment = 500_00L<Cent> // $500
            ResidualValue = 1000_00L<Cent> // $1,000
            PurchaseOption = Some 1000_00L<Cent>
            ImplicitRate = FSharp.Finance.Personal.Interest.Rate.Annual (FSharp.Finance.Personal.Calculation.Percent 4.0m)
        }

        let details = Lease.calculateLeaseDetails terms

        details.LeasePayment |> should equal 20000L<Cent>
        details.TotalPayments |> should equal 530000L<Cent> // $200*24 + $500
        details.TotalCost |> should equal 630000L<Cent> // Total payments + purchase option
        details.PresentValue |> should be (greaterThan 0L<Cent>)

    [<Fact>]
    let ``Lease schedule has correct length`` () =
        let terms = {
            EquipmentDescription = "Computer"
            FairMarketValue = 3000_00L<Cent> // $3,000
            TermMonths = 12
            LeaseType = Lease.LeaseType.FinanceLease
            PaymentFrequency = Lease.PaymentFrequency.Monthly
            PaymentTiming = Lease.PaymentTiming.InArrears
            LeasePayment = 213_00L<Cent> // ~level rental consistent with the residual
            PeriodicFee = None
            UpfrontPayment = 0L<Cent>
            ResidualValue = 500_00L<Cent> // $500
            PurchaseOption = None
            ImplicitRate = FSharp.Finance.Personal.Interest.Rate.Annual (FSharp.Finance.Personal.Calculation.Percent 3.0m)
        }

        let startDate = FSharp.Finance.Personal.DateDay.Date(2024, 1, 1)
        let schedule = Lease.generateLeaseSchedule terms startDate

        schedule.Length |> should equal 12

        // First payment should have payment number 1
        schedule.[0].PaymentNumber |> should equal 1

        // Last payment should have payment number equal to term
        schedule.[11].PaymentNumber |> should equal 12

        schedule.[11].RemainingLiability |> should equal 500_00L<Cent>
        schedule.[11].PaymentAmount |> should be (lessThanOrEqualTo 250_00L<Cent>)

    [<Fact>]
    let ``Operating lease does not split principal and interest`` () =
        let terms = {
            EquipmentDescription = "Equipment"
            FairMarketValue = 6000_00L<Cent> // $6,000
            TermMonths = 24
            LeaseType = Lease.LeaseType.OperatingLease
            PaymentFrequency = Lease.PaymentFrequency.Monthly
            PaymentTiming = Lease.PaymentTiming.InArrears
            LeasePayment = 250_00L<Cent> // $250/month
            PeriodicFee = None
            UpfrontPayment = 0L<Cent>
            ResidualValue = 1000_00L<Cent>
            PurchaseOption = None
            ImplicitRate = FSharp.Finance.Personal.Interest.Rate.Annual (FSharp.Finance.Personal.Calculation.Percent 4.0m)
        }

        let startDate = FSharp.Finance.Personal.DateDay.Date(2024, 1, 1)
        let schedule = Lease.generateLeaseSchedule terms startDate

        // Operating leases should not split principal/interest
        schedule |> Array.iter (fun item ->
            item.PrincipalPortion |> should equal 0L<Cent>
            item.InterestPortion |> should equal 0L<Cent>)

    [<Fact>]
    let ``Finance lease splits principal and interest`` () =
        let terms = {
            EquipmentDescription = "Equipment"
            FairMarketValue = 6000_00L<Cent> // $6,000
            TermMonths = 24
            LeaseType = Lease.LeaseType.FinanceLease
            PaymentFrequency = Lease.PaymentFrequency.Monthly
            PaymentTiming = Lease.PaymentTiming.InArrears
            LeasePayment = 220_45L<Cent> // ~level rental consistent with the residual
            PeriodicFee = None
            UpfrontPayment = 0L<Cent>
            ResidualValue = 1000_00L<Cent>
            PurchaseOption = None
            ImplicitRate = FSharp.Finance.Personal.Interest.Rate.Annual (FSharp.Finance.Personal.Calculation.Percent 4.0m)
        }

        let startDate = FSharp.Finance.Personal.DateDay.Date(2024, 1, 1)
        let schedule = Lease.generateLeaseSchedule terms startDate

        // Finance leases should split principal and interest
        let firstPayment = schedule.[0]
        firstPayment.PrincipalPortion |> should be (greaterThan 0L<Cent>)
        firstPayment.InterestPortion |> should be (greaterThan 0L<Cent>)

        // Principal + interest should equal payment amount
        (firstPayment.PrincipalPortion + firstPayment.InterestPortion) |> should equal firstPayment.PaymentAmount

    [<Fact>]
    let ``Finance lease schedule amortizes to residual not zero`` () =
        let terms = {
            EquipmentDescription = "Equipment"
            FairMarketValue = 6000_00L<Cent>
            TermMonths = 24
            LeaseType = Lease.LeaseType.FinanceLease
            PaymentFrequency = Lease.PaymentFrequency.Monthly
            PaymentTiming = Lease.PaymentTiming.InArrears
            LeasePayment = 220_45L<Cent> // ~level rental consistent with the residual
            PeriodicFee = None
            UpfrontPayment = 0L<Cent>
            ResidualValue = 1000_00L<Cent>
            PurchaseOption = None
            ImplicitRate = FSharp.Finance.Personal.Interest.Rate.Annual (Percent 4.0m)
        }

        let startDate = FSharp.Finance.Personal.DateDay.Date(2024, 1, 1)
        let schedule = Lease.generateLeaseSchedule terms startDate
        let last = schedule |> Array.last

        last.RemainingLiability |> should equal 1000_00L<Cent>
        last.PaymentAmount |> should be (lessThanOrEqualTo 250_00L<Cent>)

    [<Fact>]
    let ``Finance lease charges the stated rental every period with a final plug`` () =
        let terms = {
            EquipmentDescription = "Equipment"
            FairMarketValue = 6000_00L<Cent>
            TermMonths = 24
            LeaseType = Lease.LeaseType.FinanceLease
            PaymentFrequency = Lease.PaymentFrequency.Monthly
            PaymentTiming = Lease.PaymentTiming.InArrears
            LeasePayment = 220_45L<Cent>
            PeriodicFee = None
            UpfrontPayment = 0L<Cent>
            ResidualValue = 1000_00L<Cent>
            PurchaseOption = None
            ImplicitRate = FSharp.Finance.Personal.Interest.Rate.Annual (Percent 4.0m)
        }

        let startDate = FSharp.Finance.Personal.DateDay.Date(2024, 1, 1)
        let schedule = Lease.generateLeaseSchedule terms startDate

        // every period except the last charges the stated rental verbatim - never rewritten
        schedule
        |> Array.take (schedule.Length - 1)
        |> Array.iter (fun item -> item.PaymentAmount |> should equal 220_45L<Cent>)

        // the final period is the plug that lands exactly on the residual
        let last = schedule |> Array.last
        last.RemainingLiability |> should equal 1000_00L<Cent>
        last.PrincipalPortion + last.InterestPortion |> should equal last.PaymentAmount

        // totals reflect the contractual schedule
        let details = Lease.calculateLeaseDetails terms
        details.TotalPayments |> should equal (schedule |> Array.sumBy (fun item -> item.PaymentAmount))

    [<Fact>]
    let ``Finance lease rejects rental too high to land on the residual`` () =
        // audited scenario: stated rental 300.00 on a contract whose consistent level rental
        // is ~220; the old code silently rewrote the rental to 3.33 for the last six periods
        let terms = {
            EquipmentDescription = "Equipment"
            FairMarketValue = 6000_00L<Cent>
            TermMonths = 24
            LeaseType = Lease.LeaseType.FinanceLease
            PaymentFrequency = Lease.PaymentFrequency.Monthly
            PaymentTiming = Lease.PaymentTiming.InArrears
            LeasePayment = 300_00L<Cent>
            PeriodicFee = None
            UpfrontPayment = 0L<Cent>
            ResidualValue = 1000_00L<Cent>
            PurchaseOption = None
            ImplicitRate = FSharp.Finance.Personal.Interest.Rate.Annual (Percent 4.0m)
        }

        let startDate = FSharp.Finance.Personal.DateDay.Date(2024, 1, 1)
        let ex =
            Assert.Throws<System.ArgumentException>(fun () ->
                Lease.generateLeaseSchedule terms startDate |> ignore)

        // the error must be descriptive, quoting the consistent level rental
        ex.Message |> should haveSubstring "too high"
        ex.Message |> should haveSubstring "220"

    [<Fact>]
    let ``Operating lease schedule carries no liability`` () =
        let terms = {
            EquipmentDescription = "Equipment"
            FairMarketValue = 6000_00L<Cent>
            TermMonths = 24
            LeaseType = Lease.LeaseType.OperatingLease
            PaymentFrequency = Lease.PaymentFrequency.Monthly
            PaymentTiming = Lease.PaymentTiming.InArrears
            LeasePayment = 250_00L<Cent>
            PeriodicFee = None
            UpfrontPayment = 0L<Cent>
            ResidualValue = 1000_00L<Cent>
            PurchaseOption = None
            ImplicitRate = FSharp.Finance.Personal.Interest.Rate.Annual (Percent 4.0m)
        }

        let startDate = FSharp.Finance.Personal.DateDay.Date(2024, 1, 1)
        let schedule = Lease.generateLeaseSchedule terms startDate

        schedule |> Array.iter (fun item -> item.RemainingLiability |> should equal 0L<Cent>)

    [<Fact>]
    let ``Lease details uses actual schedule totals and present value`` () =
        let terms = {
            EquipmentDescription = "Computer"
            FairMarketValue = 3000_00L<Cent>
            TermMonths = 12
            LeaseType = Lease.LeaseType.FinanceLease
            PaymentFrequency = Lease.PaymentFrequency.Monthly
            PaymentTiming = Lease.PaymentTiming.InArrears
            LeasePayment = 213_00L<Cent> // ~level rental consistent with the residual
            PeriodicFee = None
            UpfrontPayment = 0L<Cent>
            ResidualValue = 500_00L<Cent>
            PurchaseOption = None
            ImplicitRate = FSharp.Finance.Personal.Interest.Rate.Annual (Percent 3.0m)
        }

        let details = Lease.calculateLeaseDetails terms
        let startDate = FSharp.Finance.Personal.DateDay.Date(2024, 1, 1)
        let schedule = Lease.generateLeaseSchedule terms startDate
        let scheduleTotal = schedule |> Array.sumBy (fun item -> item.PaymentAmount)

        details.TotalPayments |> should equal scheduleTotal
        details.PresentValue |> should be (lessThanOrEqualTo details.TotalPayments)

    [<Fact>]
    let ``In-advance rental is lower than in-arrears by the annuity-due factor`` () =
        let baseTerms = {
            EquipmentDescription = "Equipment"
            FairMarketValue = 10000_00L<Cent>
            TermMonths = 36
            LeaseType = Lease.LeaseType.FinanceLease
            PaymentFrequency = Lease.PaymentFrequency.Monthly
            PaymentTiming = Lease.PaymentTiming.InArrears
            LeasePayment = 0L<Cent>
            PeriodicFee = None
            UpfrontPayment = 0L<Cent>
            ResidualValue = 0L<Cent>
            PurchaseOption = None
            ImplicitRate = FSharp.Finance.Personal.Interest.Rate.Annual (Percent 6.0m)
        }

        let arrearsPayment = Lease.calculateLeasePayment baseTerms
        let advancePayment = Lease.calculateLeasePayment { baseTerms with PaymentTiming = Lease.PaymentTiming.InAdvance }

        advancePayment |> should be (lessThan arrearsPayment)

        // annuity-due factor: payment(due) = payment(ordinary) / (1 + period rate)
        let expected = Cent.fromDecimal (Cent.toDecimal arrearsPayment / (1m + 0.06m / 12m))
        abs (advancePayment - expected) |> should be (lessThanOrEqualTo 1L<Cent>)

    [<Fact>]
    let ``In-advance finance lease schedule lands on the residual`` () =
        let terms = {
            EquipmentDescription = "Equipment"
            FairMarketValue = 6000_00L<Cent>
            TermMonths = 24
            LeaseType = Lease.LeaseType.FinanceLease
            PaymentFrequency = Lease.PaymentFrequency.Monthly
            PaymentTiming = Lease.PaymentTiming.InAdvance
            LeasePayment = 0L<Cent>
            PeriodicFee = None
            UpfrontPayment = 0L<Cent>
            ResidualValue = 1000_00L<Cent>
            PurchaseOption = None
            ImplicitRate = FSharp.Finance.Personal.Interest.Rate.Annual (Percent 4.0m)
        }

        let startDate = FSharp.Finance.Personal.DateDay.Date(2024, 1, 1)
        let schedule = Lease.generateLeaseSchedule terms startDate

        schedule.Length |> should equal 24

        // first payment is due at the start date (in advance)
        schedule.[0].PaymentDate |> should equal startDate

        // rows are internally consistent
        schedule |> Array.iter (fun item ->
            item.PrincipalPortion + item.InterestPortion |> should equal item.PaymentAmount)

        // the schedule lands exactly on the residual and repays principal net of residual
        let last = schedule |> Array.last
        last.RemainingLiability |> should equal 1000_00L<Cent>
        schedule |> Array.sumBy (fun item -> item.PrincipalPortion) |> should equal 5000_00L<Cent>

    [<Fact>]
    let ``Finance lease rejects rental below period interest`` () =
        let terms = {
            EquipmentDescription = "Equipment"
            FairMarketValue = 10000_00L<Cent>
            TermMonths = 24
            LeaseType = Lease.LeaseType.FinanceLease
            PaymentFrequency = Lease.PaymentFrequency.Monthly
            PaymentTiming = Lease.PaymentTiming.InArrears
            LeasePayment = 1_00L<Cent>
            PeriodicFee = None
            UpfrontPayment = 0L<Cent>
            ResidualValue = 1000_00L<Cent>
            PurchaseOption = None
            ImplicitRate = FSharp.Finance.Personal.Interest.Rate.Annual (Percent 12.0m)
        }

        let startDate = FSharp.Finance.Personal.DateDay.Date(2024, 1, 1)
        (fun () -> Lease.generateLeaseSchedule terms startDate |> ignore)
        |> should throw typeof<System.ArgumentException>

    [<Fact>]
    let ``Finance lease rejects rental that cannot reach residual by maturity`` () =
        let terms = {
            EquipmentDescription = "Equipment"
            FairMarketValue = 6000_00L<Cent>
            TermMonths = 24
            LeaseType = Lease.LeaseType.FinanceLease
            PaymentFrequency = Lease.PaymentFrequency.Monthly
            PaymentTiming = Lease.PaymentTiming.InArrears
            LeasePayment = 50_00L<Cent>
            PeriodicFee = None
            UpfrontPayment = 0L<Cent>
            ResidualValue = 1000_00L<Cent>
            PurchaseOption = None
            ImplicitRate = FSharp.Finance.Personal.Interest.Rate.Annual (Percent 4.0m)
        }

        let startDate = FSharp.Finance.Personal.DateDay.Date(2024, 1, 1)
        let ex =
            Assert.Throws<System.ArgumentException>(fun () ->
                Lease.generateLeaseSchedule terms startDate |> ignore)

        ex.Message |> should haveSubstring "too low"
        ex.Message |> should haveSubstring "minimum consistent level rental"

    [<Fact>]
    let ``Lease rejects upfront payment at or above fair value`` () =
        let terms = {
            EquipmentDescription = "Equipment"
            FairMarketValue = 6000_00L<Cent>
            TermMonths = 24
            LeaseType = Lease.LeaseType.FinanceLease
            PaymentFrequency = Lease.PaymentFrequency.Monthly
            PaymentTiming = Lease.PaymentTiming.InArrears
            LeasePayment = 0L<Cent>
            PeriodicFee = None
            UpfrontPayment = 6000_00L<Cent>
            ResidualValue = 1000_00L<Cent>
            PurchaseOption = None
            ImplicitRate = FSharp.Finance.Personal.Interest.Rate.Annual (Percent 4.0m)
        }

        (fun () -> Lease.calculateLeasePayment terms |> ignore)
        |> should throw typeof<System.ArgumentException>

    [<Fact>]
    let ``Lease vs buy analysis includes depreciation schedule`` () =
        let terms = {
            EquipmentDescription = "Manufacturing equipment"
            FairMarketValue = 10000_00L<Cent> // $10,000
            TermMonths = 36
            LeaseType = Lease.LeaseType.FinanceLease
            PaymentFrequency = Lease.PaymentFrequency.Monthly
            PaymentTiming = Lease.PaymentTiming.InArrears
            LeasePayment = 218_14L<Cent> // ~level rental consistent with the residual
            PeriodicFee = None
            UpfrontPayment = 1000_00L<Cent>
            ResidualValue = 2000_00L<Cent>
            PurchaseOption = Some 2000_00L<Cent>
            ImplicitRate = FSharp.Finance.Personal.Interest.Rate.Annual (FSharp.Finance.Personal.Calculation.Percent 5.0m)
        }

        let startDate = FSharp.Finance.Personal.DateDay.Date(2024, 1, 1)
        let analysis = Lease.analyzeLeaseVsBuy terms startDate (Percent 8.0m)

        analysis.LeaseDetails.LeasePayment |> should equal 218_14L<Cent>
        analysis.PurchaseDepreciation |> should not' (be Empty)
        analysis.LeaseSchedule.Length |> should equal 36

        // "manufacturing equipment" is a recognized 7-year classification, not an assumption
        analysis.AssetClassAssumed |> should equal false

        // discounting a 5% lease at 8% makes leasing advantageous: NAL must be positive
        analysis.NetAdvantageToLeasing |> should be (greaterThan 0L<Cent>)

        // discounting at the implicit rate makes both alternatives nearly equivalent
        let atImplicit = Lease.analyzeLeaseVsBuy terms startDate (Percent 5.0m)
        abs atImplicit.NetAdvantageToLeasing |> should be (lessThan 5_00L<Cent>)

    [<Fact>]
    let ``Recurring periodic fee appears as its own column and in totals`` () =
        let baseTerms = {
            EquipmentDescription = "Equipment"
            FairMarketValue = 6000_00L<Cent>
            TermMonths = 24
            LeaseType = Lease.LeaseType.FinanceLease
            PaymentFrequency = Lease.PaymentFrequency.Monthly
            PaymentTiming = Lease.PaymentTiming.InArrears
            LeasePayment = 220_45L<Cent>
            PeriodicFee = None
            UpfrontPayment = 0L<Cent>
            ResidualValue = 1000_00L<Cent>
            PurchaseOption = None
            ImplicitRate = FSharp.Finance.Personal.Interest.Rate.Annual (Percent 4.0m)
        }
        let terms = { baseTerms with PeriodicFee = Some 15_00L<Cent> }

        let startDate = FSharp.Finance.Personal.DateDay.Date(2024, 1, 1)
        let schedule = Lease.generateLeaseSchedule terms startDate

        // the fee is identifiable in every row: payment = principal + interest + fee
        schedule |> Array.iter (fun item ->
            item.FeePortion |> should equal 15_00L<Cent>
            item.PaymentAmount |> should equal (item.PrincipalPortion + item.InterestPortion + item.FeePortion))

        // non-final payments are the stated rental plus the fee
        schedule
        |> Array.take (schedule.Length - 1)
        |> Array.iter (fun item -> item.PaymentAmount |> should equal (220_45L<Cent> + 15_00L<Cent>))

        // the fee does not change the liability amortization
        let noFeeSchedule = Lease.generateLeaseSchedule baseTerms startDate
        Array.zip schedule noFeeSchedule
        |> Array.iter (fun (feeItem, noFeeItem) ->
            feeItem.PrincipalPortion |> should equal noFeeItem.PrincipalPortion
            feeItem.InterestPortion |> should equal noFeeItem.InterestPortion
            feeItem.RemainingLiability |> should equal noFeeItem.RemainingLiability)

        // totals include the fees, reported separately
        let details = Lease.calculateLeaseDetails terms
        let noFeeDetails = Lease.calculateLeaseDetails baseTerms
        details.TotalFees |> should equal (24L * 15_00L<Cent>)
        details.TotalPayments |> should equal (noFeeDetails.TotalPayments + details.TotalFees)

    [<Fact>]
    let ``Lease rejects term incompatible with payment frequency`` () =
        let terms = {
            EquipmentDescription = "Equipment"
            FairMarketValue = 6000_00L<Cent>
            TermMonths = 13
            LeaseType = Lease.LeaseType.FinanceLease
            PaymentFrequency = Lease.PaymentFrequency.Quarterly
            PaymentTiming = Lease.PaymentTiming.InArrears
            LeasePayment = 0L<Cent>
            PeriodicFee = None
            UpfrontPayment = 0L<Cent>
            ResidualValue = 1000_00L<Cent>
            PurchaseOption = None
            ImplicitRate = FSharp.Finance.Personal.Interest.Rate.Annual (Percent 4.0m)
        }

        (fun () -> Lease.calculateLeasePayment terms |> ignore)
        |> should throw typeof<System.ArgumentException>
