namespace FSharp.Finance.Personal.Tests

open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal

module VehicleFinanceTests =

    open Calculation
    open DateDay
    open VehicleFinance

    // ----------------------------------------------------------------
    // Helpers
    // ----------------------------------------------------------------

    /// Compares two Percent values, working around the FsUnit struct-DU reflection bug
    let percentEquals (Percent actual) (Percent expected) =
        (actual = expected) |> should equal true

    // ================================================================
    // 1. Personal Contract Purchase (PCP)
    // ================================================================

    module PcpTests =

        /// Typical UK PCP: £20,000 car, £2,000 deposit, £8,000 GFV, 9.9% p.a., 36 months
        let parameters1: Pcp.Parameters = {
            AgreementDate = Date(2025, 1, 1)
            VehiclePrice = 20_000_00L<Cent>
            Deposit = 2_000_00L<Cent>
            GuaranteedFutureValue = 8_000_00L<Cent>
            AnnualInterestRate = Percent 9.9m
            TermMonths = 36
            FirstPaymentDate = Date(2025, 2, 1)
            AprMethod = Apr.CalculationMethod.UnitedKingdom 3
        }

        [<Fact>]
        let ``PCP advance equals vehicle price minus deposit`` () =
            let result = Pcp.calculate parameters1
            result.Advance |> should equal 18_000_00L<Cent>

        [<Fact>]
        let ``PCP monthly payment is positive`` () =
            let result = Pcp.calculate parameters1
            result.MonthlyPayment |> should be (greaterThan 0L<Cent>)

        [<Fact>]
        let ``PCP total monthly payments equals monthly payment times term`` () =
            let result = Pcp.calculate parameters1
            result.TotalMonthlyPayments |> should equal (result.MonthlyPayment * 36L)

        [<Fact>]
        let ``PCP total amount payable equals deposit plus total monthly payments plus GFV`` () =
            let result = Pcp.calculate parameters1
            let expected =
                parameters1.Deposit + result.TotalMonthlyPayments + parameters1.GuaranteedFutureValue
            result.TotalAmountPayable |> should equal expected

        [<Fact>]
        let ``PCP total cost of credit equals total amount payable minus vehicle price`` () =
            let result = Pcp.calculate parameters1
            let expected = result.TotalAmountPayable - parameters1.VehiclePrice
            result.TotalCostOfCredit |> should equal expected

        [<Fact>]
        let ``PCP APR is positive`` () =
            let result = Pcp.calculate parameters1
            let (Percent apr) = result.Apr
            apr |> should be (greaterThan 0m)

        [<Fact>]
        let ``PCP monthly payment is consistent with standard PMT formula`` () =
            // manually compute PMT: (PV - FV/(1+r)^n) * r / (1 - (1+r)^-n)
            let advance = 18_000m   // £
            let gfv = 8_000m
            let r = 0.099m / 12m
            let n = 36
            let nthPower = decimal (System.Math.Pow(double (1m + r), double n))
            let discountFactor = 1m / nthPower
            let pmt = (advance - gfv * discountFactor) * r / (1m - discountFactor)
            let expected = Cent.fromDecimal pmt
            let result = Pcp.calculate parameters1
            result.MonthlyPayment |> should equal expected

        /// PCP with zero GFV (full repayment, equivalent to HP)
        [<Fact>]
        let ``PCP with zero GFV produces same payment as equivalent HP`` () =
            let pcpParams: Pcp.Parameters = {
                AgreementDate = Date(2025, 1, 1)
                VehiclePrice = 10_000_00L<Cent>
                Deposit = 1_000_00L<Cent>
                GuaranteedFutureValue = 0L<Cent>
                AnnualInterestRate = Percent 6.0m
                TermMonths = 24
                FirstPaymentDate = Date(2025, 2, 1)
                AprMethod = Apr.CalculationMethod.UnitedKingdom 3
            }
            let hpParams: HirePurchase.Parameters = {
                AgreementDate = Date(2025, 1, 1)
                VehiclePrice = 10_000_00L<Cent>
                Deposit = 1_000_00L<Cent>
                AnnualInterestRate = Percent 6.0m
                TermMonths = 24
                OptionToPurchaseFee = 0L<Cent>
                FirstPaymentDate = Date(2025, 2, 1)
                AprMethod = Apr.CalculationMethod.UnitedKingdom 3
            }
            let pcpResult = Pcp.calculate pcpParams
            let hpResult = HirePurchase.calculate hpParams
            pcpResult.MonthlyPayment |> should equal hpResult.MonthlyPayment

        /// PCP with zero interest rate
        [<Fact>]
        let ``PCP with zero interest rate spreads depreciation evenly`` () =
            let p: Pcp.Parameters = {
                AgreementDate = Date(2025, 1, 1)
                VehiclePrice = 12_000_00L<Cent>
                Deposit = 0L<Cent>
                GuaranteedFutureValue = 6_000_00L<Cent>
                AnnualInterestRate = Percent 0m
                TermMonths = 24
                FirstPaymentDate = Date(2025, 2, 1)
                AprMethod = Apr.CalculationMethod.UnitedKingdom 3
            }
            let result = Pcp.calculate p
            // depreciation = 12000 - 6000 = 6000, spread over 24 months = 250/month
            result.MonthlyPayment |> should equal 250_00L<Cent>

    // ================================================================
    // 2. Hire Purchase (HP)
    // ================================================================

    module HirePurchaseTests =

        /// Typical UK HP: £15,000 car, £1,500 deposit, 7.9% p.a., 48 months, £1 OTP fee
        let parameters1: HirePurchase.Parameters = {
            AgreementDate = Date(2025, 3, 1)
            VehiclePrice = 15_000_00L<Cent>
            Deposit = 1_500_00L<Cent>
            AnnualInterestRate = Percent 7.9m
            TermMonths = 48
            OptionToPurchaseFee = 1_00L<Cent>
            FirstPaymentDate = Date(2025, 4, 1)
            AprMethod = Apr.CalculationMethod.UnitedKingdom 3
        }

        [<Fact>]
        let ``HP advance equals vehicle price minus deposit`` () =
            let result = HirePurchase.calculate parameters1
            result.Advance |> should equal 13_500_00L<Cent>

        [<Fact>]
        let ``HP monthly payment is positive`` () =
            let result = HirePurchase.calculate parameters1
            result.MonthlyPayment |> should be (greaterThan 0L<Cent>)

        [<Fact>]
        let ``HP total monthly payments equals monthly payment times term`` () =
            let result = HirePurchase.calculate parameters1
            result.TotalMonthlyPayments |> should equal (result.MonthlyPayment * 48L)

        [<Fact>]
        let ``HP total amount payable includes OTP fee`` () =
            let result = HirePurchase.calculate parameters1
            let expected =
                parameters1.Deposit + result.TotalMonthlyPayments + parameters1.OptionToPurchaseFee
            result.TotalAmountPayable |> should equal expected

        [<Fact>]
        let ``HP total cost of credit equals total amount payable minus vehicle price`` () =
            let result = HirePurchase.calculate parameters1
            result.TotalCostOfCredit |> should equal (result.TotalAmountPayable - parameters1.VehiclePrice)

        [<Fact>]
        let ``HP APR is positive`` () =
            let result = HirePurchase.calculate parameters1
            let (Percent apr) = result.Apr
            apr |> should be (greaterThan 0m)

        [<Fact>]
        let ``HP monthly payment is consistent with standard PMT formula`` () =
            // PMT = PV * r / (1 - (1+r)^-n)
            let advance = 135m   // £ (using small numbers for clarity)
            let r = 0.079m / 12m
            let n = 48
            let nthPower = decimal (System.Math.Pow(double (1m + r), double n))
            let discountFactor = 1m / nthPower
            let pmt = advance * r / (1m - discountFactor)
            // scale back to real parameters: advance = £13,500
            let scaledPmt = pmt * (13_500m / 135m)
            let expected = Cent.fromDecimal scaledPmt
            let result = HirePurchase.calculate parameters1
            result.MonthlyPayment |> should equal expected

        /// HP with no OTP fee
        [<Fact>]
        let ``HP without OTP fee total amount payable excludes any fee`` () =
            let p: HirePurchase.Parameters = {
                parameters1 with
                    OptionToPurchaseFee = 0L<Cent>
            }
            let result = HirePurchase.calculate p
            result.TotalAmountPayable
            |> should equal (p.Deposit + result.TotalMonthlyPayments)

        /// HP with zero interest rate
        [<Fact>]
        let ``HP with zero interest rate splits principal evenly`` () =
            let p: HirePurchase.Parameters = {
                AgreementDate = Date(2025, 1, 1)
                VehiclePrice = 12_000_00L<Cent>
                Deposit = 0L<Cent>
                AnnualInterestRate = Percent 0m
                TermMonths = 12
                OptionToPurchaseFee = 0L<Cent>
                FirstPaymentDate = Date(2025, 2, 1)
                AprMethod = Apr.CalculationMethod.UnitedKingdom 3
            }
            let result = HirePurchase.calculate p
            result.MonthlyPayment |> should equal 1_000_00L<Cent>

    // ================================================================
    // 3. Finance Lease
    // ================================================================

    module FinanceLeaseTests =

        /// Typical finance lease: £25,000 asset, £2,500 initial rental, £5,000 residual, 8.5% p.a., 36 months
        let parameters1: FinanceLease.Parameters = {
            AgreementDate = Date(2025, 6, 1)
            AssetValue = 25_000_00L<Cent>
            InitialRental = 2_500_00L<Cent>
            ResidualValue = 5_000_00L<Cent>
            AnnualInterestRate = Percent 8.5m
            TermMonths = 36
            FirstPaymentDate = Date(2025, 7, 1)
            AprMethod = Apr.CalculationMethod.UnitedKingdom 3
        }

        [<Fact>]
        let ``Finance lease financed amount equals asset value minus initial rental`` () =
            let result = FinanceLease.calculate parameters1
            result.FinancedAmount |> should equal 22_500_00L<Cent>

        [<Fact>]
        let ``Finance lease monthly rental is positive`` () =
            let result = FinanceLease.calculate parameters1
            result.MonthlyRental |> should be (greaterThan 0L<Cent>)

        [<Fact>]
        let ``Finance lease residual payment equals parameter residual value`` () =
            let result = FinanceLease.calculate parameters1
            result.ResidualPayment |> should equal parameters1.ResidualValue

        [<Fact>]
        let ``Finance lease total rentals equals initial rental plus regular rentals`` () =
            let result = FinanceLease.calculate parameters1
            let expected = parameters1.InitialRental + result.MonthlyRental * 36L
            result.TotalRentals |> should equal expected

        [<Fact>]
        let ``Finance lease total amount payable equals total rentals plus residual`` () =
            let result = FinanceLease.calculate parameters1
            result.TotalAmountPayable
            |> should equal (result.TotalRentals + parameters1.ResidualValue)

        [<Fact>]
        let ``Finance lease APR is positive`` () =
            let result = FinanceLease.calculate parameters1
            let (Percent apr) = result.Apr
            apr |> should be (greaterThan 0m)

        /// Finance lease with zero residual value is equivalent to an HP agreement
        [<Fact>]
        let ``Finance lease with zero residual behaves like HP`` () =
            let flParams: FinanceLease.Parameters = {
                AgreementDate = Date(2025, 1, 1)
                AssetValue = 10_000_00L<Cent>
                InitialRental = 0L<Cent>
                ResidualValue = 0L<Cent>
                AnnualInterestRate = Percent 6.0m
                TermMonths = 24
                FirstPaymentDate = Date(2025, 2, 1)
                AprMethod = Apr.CalculationMethod.UnitedKingdom 3
            }
            let hpParams: HirePurchase.Parameters = {
                AgreementDate = Date(2025, 1, 1)
                VehiclePrice = 10_000_00L<Cent>
                Deposit = 0L<Cent>
                AnnualInterestRate = Percent 6.0m
                TermMonths = 24
                OptionToPurchaseFee = 0L<Cent>
                FirstPaymentDate = Date(2025, 2, 1)
                AprMethod = Apr.CalculationMethod.UnitedKingdom 3
            }
            let flResult = FinanceLease.calculate flParams
            let hpResult = HirePurchase.calculate hpParams
            flResult.MonthlyRental |> should equal hpResult.MonthlyPayment

        /// Finance lease with zero interest rate
        [<Fact>]
        let ``Finance lease with zero interest rate spreads net cost evenly`` () =
            let p: FinanceLease.Parameters = {
                AgreementDate = Date(2025, 1, 1)
                AssetValue = 24_000_00L<Cent>
                InitialRental = 0L<Cent>
                ResidualValue = 6_000_00L<Cent>
                AnnualInterestRate = Percent 0m
                TermMonths = 24
                FirstPaymentDate = Date(2025, 2, 1)
                AprMethod = Apr.CalculationMethod.UnitedKingdom 3
            }
            let result = FinanceLease.calculate p
            // net amount to finance over 24 months = 24000 - 6000 = 18000 → 750/month
            result.MonthlyRental |> should equal 750_00L<Cent>

    // ================================================================
    // 4. Add-on Insurance (PPI / GAP)
    // ================================================================

    module AddOnInsuranceTests =

        /// Typical HP with bundled GAP insurance: £15,000 advance, £1,200 GAP premium, 7.9% p.a., 48 months
        let parameters1: AddOnInsurance.Parameters = {
            AgreementDate = Date(2025, 3, 1)
            Advance = 15_000_00L<Cent>
            InsurancePremium = 1_200_00L<Cent>
            AnnualInterestRate = Percent 7.9m
            TermMonths = 48
            FirstPaymentDate = Date(2025, 4, 1)
            AprMethod = Apr.CalculationMethod.UnitedKingdom 3
        }

        [<Fact>]
        let ``Add-on total advance equals base advance plus premium`` () =
            let result = AddOnInsurance.calculate parameters1
            result.TotalAdvance |> should equal (parameters1.Advance + parameters1.InsurancePremium)

        [<Fact>]
        let ``Add-on monthly payment with insurance is higher than without`` () =
            let result = AddOnInsurance.calculate parameters1
            result.MonthlyPaymentWithInsurance
            |> should be (greaterThan result.MonthlyPaymentWithoutInsurance)

        [<Fact>]
        let ``Add-on true APR equals base APR when premium is zero`` () =
            let p: AddOnInsurance.Parameters = {
                parameters1 with InsurancePremium = 0L<Cent>
            }
            let result = AddOnInsurance.calculate p
            let (Percent baseRate) = result.BaseApr
            let (Percent trueRate) = result.TrueApr
            // with zero premium the two APRs should be the same
            baseRate |> should equal trueRate

        [<Fact>]
        let ``Add-on APR uplift is zero when premium is zero`` () =
            let p: AddOnInsurance.Parameters = {
                parameters1 with InsurancePremium = 0L<Cent>
            }
            let result = AddOnInsurance.calculate p
            let (Percent uplift) = result.AprUplift
            uplift |> should equal 0m

        [<Fact>]
        let ``Add-on APR uplift is positive when premium is positive`` () =
            let result = AddOnInsurance.calculate parameters1
            let (Percent uplift) = result.AprUplift
            uplift |> should be (greaterThan 0m)

        [<Fact>]
        let ``Add-on APR uplift equals true APR minus base APR`` () =
            let result = AddOnInsurance.calculate parameters1
            let (Percent baseRate) = result.BaseApr
            let (Percent trueRate) = result.TrueApr
            let (Percent uplift) = result.AprUplift
            uplift |> should equal (trueRate - baseRate)

        [<Fact>]
        let ``Add-on base APR is positive`` () =
            let result = AddOnInsurance.calculate parameters1
            let (Percent baseRate) = result.BaseApr
            baseRate |> should be (greaterThan 0m)

        [<Fact>]
        let ``Add-on true APR is greater than base APR`` () =
            let result = AddOnInsurance.calculate parameters1
            let (Percent baseRate) = result.BaseApr
            let (Percent trueRate) = result.TrueApr
            trueRate |> should be (greaterThan baseRate)
