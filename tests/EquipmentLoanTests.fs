namespace FSharp.Finance.Personal.Tests

open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal.Calculation
open FSharp.Finance.Personal.EquipmentFinance
open FSharp.Finance.Personal.EquipmentFinance.Loan

module EquipmentLoanTests =

    [<Fact>]
    let ``Monthly payment calculation works for zero interest`` () =
        let terms = {
            Principal = 1000_00L<Cent> // $1,000
            InterestRate = FSharp.Finance.Personal.Interest.Rate.Zero
            TermMonths = 12
            MonthlyPayment = None
            MonthlyFee = None
            EquipmentDescription = "Test equipment"
            EquipmentCost = 1000_00L<Cent>
            DownPayment = 0L<Cent>
            ResidualValue = 0L<Cent>
        }
        
        let payment = Loan.calculateMonthlyPayment terms
        payment |> should equal 8333L<Cent> // $1000/12 ≈ $83.33

    [<Fact>]
    let ``Monthly payment calculation works with interest`` () =
        let terms = {
            Principal = 10000_00L<Cent> // $10,000
            InterestRate = FSharp.Finance.Personal.Interest.Rate.Annual (FSharp.Finance.Personal.Calculation.Percent 6.0m)
            TermMonths = 36
            MonthlyPayment = None
            MonthlyFee = None
            EquipmentDescription = "Manufacturing equipment"
            EquipmentCost = 10000_00L<Cent>
            DownPayment = 0L<Cent>
            ResidualValue = 0L<Cent>
        }
        
        let payment = Loan.calculateMonthlyPayment terms
        // Payment should be greater than simple division due to interest
        payment |> should be (greaterThan 27777L<Cent>) // $10000/36

    [<Fact>]
    let ``Monthly payment discounts balloon residual at non-zero interest`` () =
        let terms = {
            Principal = 10000_00L<Cent>
            InterestRate = FSharp.Finance.Personal.Interest.Rate.Annual (Percent 6.0m)
            TermMonths = 36
            MonthlyPayment = None
            MonthlyFee = None
            EquipmentDescription = "Manufacturing equipment"
            EquipmentCost = 10000_00L<Cent>
            DownPayment = 0L<Cent>
            ResidualValue = 2000_00L<Cent>
        }

        let payment = Loan.calculateMonthlyPayment terms
        let r = 0.06m / 12m
        let n = 36
        let growth = System.Math.Pow(float (1m + r), float n) |> decimal
        let pvResidual = 2000.00m / growth
        let amortizedPrincipal = 10000.00m - pvResidual
        let expected = amortizedPrincipal * r * growth / (growth - 1m) |> Cent.fromDecimal

        abs (payment - expected) |> should be (lessThanOrEqualTo 1L<Cent>)

    [<Fact>]
    let ``Payment details calculation includes total payments and interest`` () =
        let terms = {
            Principal = 5000_00L<Cent> // $5,000
            InterestRate = FSharp.Finance.Personal.Interest.Rate.Annual (FSharp.Finance.Personal.Calculation.Percent 5.0m)
            TermMonths = 24
            MonthlyPayment = None
            MonthlyFee = None
            EquipmentDescription = "Office equipment"
            EquipmentCost = 5000_00L<Cent>
            DownPayment = 0L<Cent>
            ResidualValue = 0L<Cent>
        }
        
        let details = Loan.calculatePaymentDetails terms
        
        details.MonthlyPayment |> should be (greaterThan 0L<Cent>)
        details.TotalPayments |> should be (greaterThan terms.Principal)
        details.TotalInterest |> should be (greaterThan 0L<Cent>)
        details.NominalAnnualRate |> should equal (FSharp.Finance.Personal.Calculation.Percent 5.0m)

    [<Fact>]
    let ``Amortization schedule has correct length`` () =
        let terms = {
            Principal = 3000_00L<Cent> // $3,000
            InterestRate = FSharp.Finance.Personal.Interest.Rate.Annual (FSharp.Finance.Personal.Calculation.Percent 4.0m)
            TermMonths = 12
            MonthlyPayment = None
            MonthlyFee = None
            EquipmentDescription = "Computer"
            EquipmentCost = 3000_00L<Cent>
            DownPayment = 0L<Cent>
            ResidualValue = 0L<Cent>
        }
        
        let startDate = FSharp.Finance.Personal.DateDay.Date(2024, 1, 1)
        let schedule = Loan.generateAmortizationSchedule terms startDate
        
        schedule.Length |> should equal 12
        
        // First payment should have payment number 1
        schedule.[0].PaymentNumber |> should equal 1
        
        // Last payment should have payment number equal to term
        schedule.[11].PaymentNumber |> should equal 12
        
        // Final balance should be 0 (or residual value)
        schedule.[11].RemainingBalance |> should equal 0L<Cent>

    [<Fact>]
    let ``Amortization schedule clears balloon on final payment`` () =
        let terms = {
            Principal = 10000_00L<Cent>
            InterestRate = FSharp.Finance.Personal.Interest.Rate.Annual (Percent 6.0m)
            TermMonths = 36
            MonthlyPayment = None
            MonthlyFee = None
            EquipmentDescription = "Manufacturing equipment"
            EquipmentCost = 10000_00L<Cent>
            DownPayment = 0L<Cent>
            ResidualValue = 1000_00L<Cent>
        }

        let startDate = FSharp.Finance.Personal.DateDay.Date(2024, 1, 1)
        let schedule = Loan.generateAmortizationSchedule terms startDate
        let last = schedule |> Array.last

        last.RemainingBalance |> should equal 0L<Cent>
        last.PaymentAmount |> should be (greaterThan (Loan.calculateMonthlyPayment terms))

    [<Fact>]
    let ``Invalid loan terms are rejected`` () =
        let invalidTerms = {
            Principal = 1000_00L<Cent>
            InterestRate = FSharp.Finance.Personal.Interest.Rate.Zero
            TermMonths = 0
            MonthlyPayment = None
            MonthlyFee = None
            EquipmentDescription = "Test equipment"
            EquipmentCost = 1000_00L<Cent>
            DownPayment = 0L<Cent>
            ResidualValue = 0L<Cent>
        }

        (fun () -> Loan.calculateMonthlyPayment invalidTerms |> ignore)
        |> should throw typeof<System.ArgumentException>

    [<Fact>]
    let ``Overpaying supplied payment terminates schedule early with non-negative balances`` () =
        let terms = {
            Principal = 10000_00L<Cent> // $10,000
            InterestRate = FSharp.Finance.Personal.Interest.Rate.Annual (Percent 6.0m)
            TermMonths = 36
            MonthlyPayment = Some 1000_00L<Cent> // $1,000/month heavily overpays
            MonthlyFee = None
            EquipmentDescription = "Manufacturing equipment"
            EquipmentCost = 10000_00L<Cent>
            DownPayment = 0L<Cent>
            ResidualValue = 0L<Cent>
        }

        let startDate = FSharp.Finance.Personal.DateDay.Date(2024, 1, 1)
        let schedule = Loan.generateAmortizationSchedule terms startDate

        // schedule must end early, well before the 36-month nominal term
        schedule.Length |> should be (lessThan 36)

        // no negative balances, interest or payments anywhere
        schedule |> Array.iter (fun item ->
            item.RemainingBalance |> should be (greaterThanOrEqualTo 0L<Cent>)
            item.InterestPayment |> should be (greaterThanOrEqualTo 0L<Cent>)
            item.PrincipalPayment |> should be (greaterThanOrEqualTo 0L<Cent>)
            item.PaymentAmount |> should be (greaterThanOrEqualTo 0L<Cent>))

        // final payment clears the balance exactly
        let last = schedule |> Array.last
        last.RemainingBalance |> should equal 0L<Cent>
        last.PaymentAmount |> should be (lessThanOrEqualTo 1000_00L<Cent>)

        // total principal repaid equals the original principal
        schedule |> Array.sumBy (fun item -> item.PrincipalPayment) |> should equal terms.Principal

    [<Fact>]
    let ``Supplied payment amortizes to residual with balloon in final payment`` () =
        let terms = {
            Principal = 10000_00L<Cent>
            InterestRate = FSharp.Finance.Personal.Interest.Rate.Annual (Percent 6.0m)
            TermMonths = 36
            MonthlyPayment = Some 250_00L<Cent>
            MonthlyFee = None
            EquipmentDescription = "Manufacturing equipment"
            EquipmentCost = 10000_00L<Cent>
            DownPayment = 0L<Cent>
            ResidualValue = 2000_00L<Cent>
        }

        let startDate = FSharp.Finance.Personal.DateDay.Date(2024, 1, 1)
        let schedule = Loan.generateAmortizationSchedule terms startDate
        let last = schedule |> Array.last

        // the residual must not be ignored: the balance amortizes down to it and the final
        // payment settles it as a balloon
        last.RemainingBalance |> should equal 0L<Cent>
        last.BalloonAmount |> should equal 2000_00L<Cent>
        last.PaymentAmount |> should be (greaterThanOrEqualTo 2000_00L<Cent>)
        last.PaymentAmount |> should equal (last.InterestPayment + last.PrincipalPayment)

        // balance never drops below the residual before the final payment
        schedule
        |> Array.take (schedule.Length - 1)
        |> Array.iter (fun item ->
            item.RemainingBalance |> should be (greaterThanOrEqualTo 2000_00L<Cent>)
            item.BalloonAmount |> should equal 0L<Cent>)

    [<Fact>]
    let ``Supplied payment below first-period interest is rejected`` () =
        let terms = {
            Principal = 10000_00L<Cent>
            InterestRate = FSharp.Finance.Personal.Interest.Rate.Annual (Percent 12.0m)
            TermMonths = 36
            MonthlyPayment = Some 50_00L<Cent> // below the $100 first-month interest
            MonthlyFee = None
            EquipmentDescription = "Manufacturing equipment"
            EquipmentCost = 10000_00L<Cent>
            DownPayment = 0L<Cent>
            ResidualValue = 0L<Cent>
        }

        let startDate = FSharp.Finance.Personal.DateDay.Date(2024, 1, 1)
        (fun () -> Loan.generateAmortizationSchedule terms startDate |> ignore)
        |> should throw typeof<System.ArgumentException>

    [<Fact>]
    let ``Interest-only balloon loan with residual equal to principal`` () =
        let terms = {
            Principal = 10000_00L<Cent>
            InterestRate = FSharp.Finance.Personal.Interest.Rate.Annual (Percent 6.0m)
            TermMonths = 36
            MonthlyPayment = None
            MonthlyFee = None
            EquipmentDescription = "Manufacturing equipment"
            EquipmentCost = 10000_00L<Cent>
            DownPayment = 0L<Cent>
            ResidualValue = 10000_00L<Cent> // residual = principal: interest-only structure
        }

        // payment = P * i / 12 = 10,000 * 6% / 12 = $50.00
        let payment = Loan.calculateMonthlyPayment terms
        abs (payment - 50_00L<Cent>) |> should be (lessThanOrEqualTo 1L<Cent>)

        let startDate = FSharp.Finance.Personal.DateDay.Date(2024, 1, 1)
        let schedule = Loan.generateAmortizationSchedule terms startDate

        schedule.Length |> should equal 36

        // no principal is repaid until the final balloon
        schedule
        |> Array.take 35
        |> Array.iter (fun item ->
            item.PrincipalPayment |> should equal 0L<Cent>
            item.RemainingBalance |> should equal 10000_00L<Cent>)

        // final payment includes the full principal as balloon
        let last = schedule |> Array.last
        last.PrincipalPayment |> should equal 10000_00L<Cent>
        last.BalloonAmount |> should equal 10000_00L<Cent>
        last.RemainingBalance |> should equal 0L<Cent>

    [<Fact>]
    let ``Residual above principal is rejected`` () =
        let terms = {
            Principal = 10000_00L<Cent>
            InterestRate = FSharp.Finance.Personal.Interest.Rate.Annual (Percent 6.0m)
            TermMonths = 36
            MonthlyPayment = None
            MonthlyFee = None
            EquipmentDescription = "Manufacturing equipment"
            EquipmentCost = 10000_00L<Cent>
            DownPayment = 0L<Cent>
            ResidualValue = 10000_01L<Cent>
        }

        (fun () -> Loan.calculateMonthlyPayment terms |> ignore)
        |> should throw typeof<System.ArgumentException>

    [<Fact>]
    let ``Recurring monthly fee appears as its own column and in totals`` () =
        let baseTerms = {
            Principal = 5000_00L<Cent>
            InterestRate = FSharp.Finance.Personal.Interest.Rate.Annual (Percent 5.0m)
            TermMonths = 24
            MonthlyPayment = None
            MonthlyFee = None
            EquipmentDescription = "Office equipment"
            EquipmentCost = 5000_00L<Cent>
            DownPayment = 0L<Cent>
            ResidualValue = 0L<Cent>
        }
        let terms = { baseTerms with MonthlyFee = Some 10_00L<Cent> }

        let startDate = FSharp.Finance.Personal.DateDay.Date(2024, 1, 1)
        let schedule = Loan.generateAmortizationSchedule terms startDate

        // the fee is identifiable in every row: payment = principal + interest + fee
        schedule |> Array.iter (fun item ->
            item.FeePayment |> should equal 10_00L<Cent>
            item.PaymentAmount |> should equal (item.PrincipalPayment + item.InterestPayment + item.FeePayment))

        // the fee does not change the amortization itself
        let noFeeSchedule = Loan.generateAmortizationSchedule baseTerms startDate
        Array.zip schedule noFeeSchedule
        |> Array.iter (fun (feeItem, noFeeItem) ->
            feeItem.PrincipalPayment |> should equal noFeeItem.PrincipalPayment
            feeItem.InterestPayment |> should equal noFeeItem.InterestPayment
            feeItem.RemainingBalance |> should equal noFeeItem.RemainingBalance)

        // totals include the fees, reported separately
        let details = Loan.calculatePaymentDetails terms
        let noFeeDetails = Loan.calculatePaymentDetails baseTerms
        details.TotalFees |> should equal (24L * 10_00L<Cent>)
        details.TotalPayments |> should equal (noFeeDetails.TotalPayments + details.TotalFees)
        details.TotalInterest |> should equal noFeeDetails.TotalInterest

    [<Fact>]
    let ``Loan analysis includes depreciation schedule`` () =
        let terms = {
            Principal = 10000_00L<Cent> // $10,000
            InterestRate = FSharp.Finance.Personal.Interest.Rate.Annual (FSharp.Finance.Personal.Calculation.Percent 6.0m)
            TermMonths = 36
            MonthlyPayment = None
            MonthlyFee = None
            EquipmentDescription = "Manufacturing equipment"
            EquipmentCost = 10000_00L<Cent>
            DownPayment = 2000_00L<Cent>
            ResidualValue = 1000_00L<Cent>
        }
        
        let startDate = FSharp.Finance.Personal.DateDay.Date(2024, 1, 1)
        let analysis = Loan.analyzeLoan terms startDate (Percent 8.0m)

        analysis.PaymentDetails.MonthlyPayment |> should be (greaterThan 0L<Cent>)
        analysis.DepreciationSchedule |> should not' (be Empty)
        analysis.AmortizationSchedule.Length |> should equal 36

        // "manufacturing equipment" is a recognized 7-year classification, not an assumption
        analysis.AssetClassAssumed |> should equal false

        // NPV of the cash outflows: between the sum of undiscounted payments and zero,
        // and at least the down payment
        analysis.NetPresentValue |> should be (greaterThan terms.DownPayment)
        analysis.NetPresentValue
        |> should be (lessThan (terms.DownPayment + analysis.PaymentDetails.TotalPayments))
