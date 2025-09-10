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
            EquipmentDescription = "Manufacturing equipment"
            EquipmentCost = 10000_00L<Cent>
            DownPayment = 0L<Cent>
            ResidualValue = 0L<Cent>
        }
        
        let payment = Loan.calculateMonthlyPayment terms
        // Payment should be greater than simple division due to interest
        payment |> should be (greaterThan 27777L<Cent>) // $10000/36

    [<Fact>]
    let ``Payment details calculation includes total payments and interest`` () =
        let terms = {
            Principal = 5000_00L<Cent> // $5,000
            InterestRate = FSharp.Finance.Personal.Interest.Rate.Annual (FSharp.Finance.Personal.Calculation.Percent 5.0m)
            TermMonths = 24
            MonthlyPayment = None
            EquipmentDescription = "Office equipment"
            EquipmentCost = 5000_00L<Cent>
            DownPayment = 0L<Cent>
            ResidualValue = 0L<Cent>
        }
        
        let details = Loan.calculatePaymentDetails terms
        
        details.MonthlyPayment |> should be (greaterThan 0L<Cent>)
        details.TotalPayments |> should be (greaterThan terms.Principal)
        details.TotalInterest |> should be (greaterThan 0L<Cent>)
        details.Apr |> should equal (FSharp.Finance.Personal.Calculation.Percent 5.0m)

    [<Fact>]
    let ``Amortization schedule has correct length`` () =
        let terms = {
            Principal = 3000_00L<Cent> // $3,000
            InterestRate = FSharp.Finance.Personal.Interest.Rate.Annual (FSharp.Finance.Personal.Calculation.Percent 4.0m)
            TermMonths = 12
            MonthlyPayment = None
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
    let ``Loan analysis includes depreciation schedule`` () =
        let terms = {
            Principal = 10000_00L<Cent> // $10,000
            InterestRate = FSharp.Finance.Personal.Interest.Rate.Annual (FSharp.Finance.Personal.Calculation.Percent 6.0m)
            TermMonths = 36
            MonthlyPayment = None
            EquipmentDescription = "Manufacturing equipment"
            EquipmentCost = 10000_00L<Cent>
            DownPayment = 2000_00L<Cent>
            ResidualValue = 1000_00L<Cent>
        }
        
        let startDate = FSharp.Finance.Personal.DateDay.Date(2024, 1, 1)
        let analysis = Loan.analyzeLoan terms startDate
        
        analysis.PaymentDetails.MonthlyPayment |> should be (greaterThan 0L<Cent>)
        analysis.DepreciationSchedule |> should not' (be Empty)
        analysis.AmortizationSchedule.Length |> should equal 36