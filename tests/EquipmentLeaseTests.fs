namespace FSharp.Finance.Personal.Tests

open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal.EquipmentFinance

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
            FairMarketValue = 120000L<FSharp.Finance.Personal.Calculation.Cent> // $1,200
            TermMonths = 12
            LeaseType = Lease.LeaseType.OperatingLease
            PaymentFrequency = Lease.PaymentFrequency.Monthly
            LeasePayment = 0L<FSharp.Finance.Personal.Calculation.Cent>
            UpfrontPayment = 0L<FSharp.Finance.Personal.Calculation.Cent>
            ResidualValue = 20000L<FSharp.Finance.Personal.Calculation.Cent> // $200
            PurchaseOption = None
            ImplicitRate = FSharp.Finance.Personal.Interest.Rate.Zero
        }
        
        let payment = Lease.calculateLeasePayment terms
        // ($1200 - $200) / 12 = $83.33
        payment |> should equal 8333L<FSharp.Finance.Personal.Calculation.Cent>

    [<Fact>]
    let ``Lease payment calculation works with interest`` () =
        let terms = {
            EquipmentDescription = "Manufacturing equipment"
            FairMarketValue = 1000000L<FSharp.Finance.Personal.Calculation.Cent> // $10,000
            TermMonths = 36
            LeaseType = Lease.LeaseType.FinanceLease
            PaymentFrequency = Lease.PaymentFrequency.Monthly
            LeasePayment = 0L<FSharp.Finance.Personal.Calculation.Cent>
            UpfrontPayment = 100000L<FSharp.Finance.Personal.Calculation.Cent> // $1,000
            ResidualValue = 200000L<FSharp.Finance.Personal.Calculation.Cent> // $2,000
            PurchaseOption = Some 200000L<FSharp.Finance.Personal.Calculation.Cent>
            ImplicitRate = FSharp.Finance.Personal.Interest.Rate.Annual (FSharp.Finance.Personal.Calculation.Percent 5.0m)
        }
        
        let payment = Lease.calculateLeasePayment terms
        // Payment should be greater than simple division due to interest
        payment |> should be (greaterThan 22222L<FSharp.Finance.Personal.Calculation.Cent>) // Simple calculation would be less

    [<Fact>]
    let ``Lease details calculation includes total cost`` () =
        let terms = {
            EquipmentDescription = "Office equipment"
            FairMarketValue = 500000L<FSharp.Finance.Personal.Calculation.Cent> // $5,000
            TermMonths = 24
            LeaseType = Lease.LeaseType.OperatingLease
            PaymentFrequency = Lease.PaymentFrequency.Monthly
            LeasePayment = 20000L<FSharp.Finance.Personal.Calculation.Cent> // $200/month
            UpfrontPayment = 50000L<FSharp.Finance.Personal.Calculation.Cent> // $500
            ResidualValue = 100000L<FSharp.Finance.Personal.Calculation.Cent> // $1,000
            PurchaseOption = Some 100000L<FSharp.Finance.Personal.Calculation.Cent>
            ImplicitRate = FSharp.Finance.Personal.Interest.Rate.Annual (FSharp.Finance.Personal.Calculation.Percent 4.0m)
        }
        
        let details = Lease.calculateLeaseDetails terms
        
        details.LeasePayment |> should equal 20000L<FSharp.Finance.Personal.Calculation.Cent>
        details.TotalPayments |> should equal 530000L<FSharp.Finance.Personal.Calculation.Cent> // $200*24 + $500
        details.TotalCost |> should equal 630000L<FSharp.Finance.Personal.Calculation.Cent> // Total payments + purchase option
        details.PresentValue |> should be (greaterThan 0L<FSharp.Finance.Personal.Calculation.Cent>)

    [<Fact>]
    let ``Lease schedule has correct length`` () =
        let terms = {
            EquipmentDescription = "Computer"
            FairMarketValue = 300000L<FSharp.Finance.Personal.Calculation.Cent> // $3,000
            TermMonths = 12
            LeaseType = Lease.LeaseType.FinanceLease
            PaymentFrequency = Lease.PaymentFrequency.Monthly
            LeasePayment = 25000L<FSharp.Finance.Personal.Calculation.Cent> // $250/month
            UpfrontPayment = 0L<FSharp.Finance.Personal.Calculation.Cent>
            ResidualValue = 50000L<FSharp.Finance.Personal.Calculation.Cent> // $500
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
        
        // All payments should have the same amount for operating lease
        schedule |> Array.iter (fun item -> 
            item.PaymentAmount |> should equal 25000L<FSharp.Finance.Personal.Calculation.Cent>)

    [<Fact>]
    let ``Operating lease does not split principal and interest`` () =
        let terms = {
            EquipmentDescription = "Equipment"
            FairMarketValue = 600000L<FSharp.Finance.Personal.Calculation.Cent> // $6,000
            TermMonths = 24
            LeaseType = Lease.LeaseType.OperatingLease
            PaymentFrequency = Lease.PaymentFrequency.Monthly
            LeasePayment = 25000L<FSharp.Finance.Personal.Calculation.Cent> // $250/month
            UpfrontPayment = 0L<FSharp.Finance.Personal.Calculation.Cent>
            ResidualValue = 100000L<FSharp.Finance.Personal.Calculation.Cent>
            PurchaseOption = None
            ImplicitRate = FSharp.Finance.Personal.Interest.Rate.Annual (FSharp.Finance.Personal.Calculation.Percent 4.0m)
        }
        
        let startDate = FSharp.Finance.Personal.DateDay.Date(2024, 1, 1)
        let schedule = Lease.generateLeaseSchedule terms startDate
        
        // Operating leases should not split principal/interest
        schedule |> Array.iter (fun item -> 
            item.PrincipalPortion |> should equal 0L<FSharp.Finance.Personal.Calculation.Cent>
            item.InterestPortion |> should equal 0L<FSharp.Finance.Personal.Calculation.Cent>)

    [<Fact>]
    let ``Finance lease splits principal and interest`` () =
        let terms = {
            EquipmentDescription = "Equipment"
            FairMarketValue = 600000L<FSharp.Finance.Personal.Calculation.Cent> // $6,000
            TermMonths = 24
            LeaseType = Lease.LeaseType.FinanceLease
            PaymentFrequency = Lease.PaymentFrequency.Monthly
            LeasePayment = 25000L<FSharp.Finance.Personal.Calculation.Cent> // $250/month
            UpfrontPayment = 0L<FSharp.Finance.Personal.Calculation.Cent>
            ResidualValue = 100000L<FSharp.Finance.Personal.Calculation.Cent>
            PurchaseOption = None
            ImplicitRate = FSharp.Finance.Personal.Interest.Rate.Annual (FSharp.Finance.Personal.Calculation.Percent 4.0m)
        }
        
        let startDate = FSharp.Finance.Personal.DateDay.Date(2024, 1, 1)
        let schedule = Lease.generateLeaseSchedule terms startDate
        
        // Finance leases should split principal and interest
        let firstPayment = schedule.[0]
        firstPayment.PrincipalPortion |> should be (greaterThan 0L<FSharp.Finance.Personal.Calculation.Cent>)
        firstPayment.InterestPortion |> should be (greaterThan 0L<FSharp.Finance.Personal.Calculation.Cent>)
        
        // Principal + interest should equal payment amount
        (firstPayment.PrincipalPortion + firstPayment.InterestPortion) |> should equal firstPayment.PaymentAmount

    [<Fact>]
    let ``Lease vs buy analysis includes depreciation schedule`` () =
        let terms = {
            EquipmentDescription = "Manufacturing equipment"
            FairMarketValue = 1000000L<FSharp.Finance.Personal.Calculation.Cent> // $10,000
            TermMonths = 36
            LeaseType = Lease.LeaseType.FinanceLease
            PaymentFrequency = Lease.PaymentFrequency.Monthly
            LeasePayment = 30000L<FSharp.Finance.Personal.Calculation.Cent> // $300/month
            UpfrontPayment = 100000L<FSharp.Finance.Personal.Calculation.Cent>
            ResidualValue = 200000L<FSharp.Finance.Personal.Calculation.Cent>
            PurchaseOption = Some 200000L<FSharp.Finance.Personal.Calculation.Cent>
            ImplicitRate = FSharp.Finance.Personal.Interest.Rate.Annual (FSharp.Finance.Personal.Calculation.Percent 5.0m)
        }
        
        let startDate = FSharp.Finance.Personal.DateDay.Date(2024, 1, 1)
        let analysis = Lease.analyzeLeaseVsBuy terms startDate
        
        analysis.LeaseDetails.LeasePayment |> should equal 30000L<FSharp.Finance.Personal.Calculation.Cent>
        analysis.PurchaseDepreciation |> should not' (be Empty)
        analysis.LeaseSchedule.Length |> should equal 36