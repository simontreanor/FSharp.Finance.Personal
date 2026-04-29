namespace FSharp.Finance.Personal

open System

/// functions for modelling UK vehicle finance products: PCP, Hire Purchase, Finance Lease, and add-on insurance cost
module VehicleFinance =

    open Calculation
    open DateDay

    // ============================================================
    // 1. Personal Contract Purchase (PCP)
    // ============================================================

    /// Personal Contract Purchase (PCP) finance calculations
    ///
    /// Monthly payments are based on the depreciation of the vehicle (purchase price less Guaranteed Future Value)
    /// plus interest on the total advance. At the end of the term the borrower can pay the GFV to own the vehicle,
    /// return it, or use it as a part-exchange deposit.
    module Pcp =

        /// parameters for a Personal Contract Purchase agreement
        [<Struct>]
        type Parameters = {
            /// the date the agreement commences
            AgreementDate: Date
            /// the on-road (total) purchase price of the vehicle
            VehiclePrice: int64<Cent>
            /// the deposit paid upfront; the finance advance is VehiclePrice − Deposit
            Deposit: int64<Cent>
            /// the Guaranteed Future Value: the balloon payment due at the end of the term
            GuaranteedFutureValue: int64<Cent>
            /// the nominal annual interest rate used to calculate monthly payments (e.g. Percent 9.9m for 9.9 % p.a.)
            AnnualInterestRate: Percent
            /// the number of regular monthly payments
            TermMonths: int
            /// the date on which the first regular monthly payment falls due
            FirstPaymentDate: Date
            /// the APR calculation method to apply
            AprMethod: Apr.CalculationMethod
        }

        /// the result of a PCP calculation
        [<Struct>]
        type Result = {
            /// the finance advance: VehiclePrice − Deposit
            Advance: int64<Cent>
            /// the level monthly payment amount
            MonthlyPayment: int64<Cent>
            /// the sum of all regular monthly payments (MonthlyPayment × TermMonths)
            TotalMonthlyPayments: int64<Cent>
            /// the total amount payable: Deposit + TotalMonthlyPayments + GuaranteedFutureValue
            TotalAmountPayable: int64<Cent>
            /// the total cost of credit: TotalAmountPayable − VehiclePrice
            TotalCostOfCredit: int64<Cent>
            /// the APR calculated using the specified calculation method, treating the GFV as a final cashflow
            Apr: Percent
        }

        /// calculates a PCP payment schedule and key statistics
        let calculate (p: Parameters) =
            let advance = p.VehiclePrice - p.Deposit
            let monthlyRate = Percent.toDecimal p.AnnualInterestRate / 12m
            let n = p.TermMonths
            let gfv = p.GuaranteedFutureValue

            // Standard PMT formula with balloon: PMT = (PV − FV/(1+r)^n) × r / (1 − (1+r)^−n)
            let monthlyPayment =
                if monthlyRate = 0m || n = 0 then
                    // zero-rate: spread depreciation evenly
                    if n = 0 then 0L<Cent>
                    else
                        (Cent.toDecimal advance - Cent.toDecimal gfv) / decimal n
                        |> Cent.fromDecimal
                else
                    let r = monthlyRate
                    let pv = Cent.toDecimal advance
                    let fv = Cent.toDecimal gfv
                    let nthPower = powi n (1m + r)
                    let discountFactor = 1m / nthPower
                    (pv - fv * discountFactor) * r / (1m - discountFactor)
                    |> Cent.fromDecimal

            let totalMonthlyPayments = monthlyPayment * int64 n
            let totalAmountPayable = p.Deposit + totalMonthlyPayments + gfv
            let totalCostOfCredit = totalAmountPayable - p.VehiclePrice

            // APR cash flows: n monthly payments plus the GFV balloon on the final payment date
            let aprTransfers =
                [|
                    for i in 1 .. n do
                        yield {
                            Apr.TransferType = Apr.Payment
                            Apr.TransferDate = p.FirstPaymentDate.AddMonths(i - 1)
                            Apr.Value = monthlyPayment
                        }
                    if gfv > 0L<Cent> then
                        yield {
                            Apr.TransferType = Apr.Payment
                            Apr.TransferDate = p.FirstPaymentDate.AddMonths(n - 1)
                            Apr.Value = gfv
                        }
                |]

            let aprSolution = Apr.calculate p.AprMethod advance p.AgreementDate aprTransfers
            let apr = Apr.toPercent p.AprMethod aprSolution

            {
                Advance = advance
                MonthlyPayment = monthlyPayment
                TotalMonthlyPayments = totalMonthlyPayments
                TotalAmountPayable = totalAmountPayable
                TotalCostOfCredit = totalCostOfCredit
                Apr = apr
            }

    // ============================================================
    // 2. Hire Purchase (HP)
    // ============================================================

    /// Hire Purchase (HP) finance calculations
    ///
    /// A simple, fully-amortising schedule. Title transfers to the borrower at the end of the term,
    /// sometimes on payment of a nominal option-to-purchase fee.
    module HirePurchase =

        /// parameters for a Hire Purchase agreement
        [<Struct>]
        type Parameters = {
            /// the date the agreement commences
            AgreementDate: Date
            /// the on-road (total) purchase price of the vehicle
            VehiclePrice: int64<Cent>
            /// the deposit paid upfront; the finance advance is VehiclePrice − Deposit
            Deposit: int64<Cent>
            /// the nominal annual interest rate used to calculate monthly payments
            AnnualInterestRate: Percent
            /// the number of regular monthly payments
            TermMonths: int
            /// a nominal option-to-purchase fee payable at the end of the term (often £0 or £1); included in APR
            OptionToPurchaseFee: int64<Cent>
            /// the date on which the first regular monthly payment falls due
            FirstPaymentDate: Date
            /// the APR calculation method to apply
            AprMethod: Apr.CalculationMethod
        }

        /// the result of an HP calculation
        [<Struct>]
        type Result = {
            /// the finance advance: VehiclePrice − Deposit
            Advance: int64<Cent>
            /// the level monthly payment amount
            MonthlyPayment: int64<Cent>
            /// the sum of all regular monthly payments (MonthlyPayment × TermMonths)
            TotalMonthlyPayments: int64<Cent>
            /// the total amount payable: Deposit + TotalMonthlyPayments + OptionToPurchaseFee
            TotalAmountPayable: int64<Cent>
            /// the total cost of credit: TotalAmountPayable − VehiclePrice
            TotalCostOfCredit: int64<Cent>
            /// the APR calculated using the specified calculation method
            Apr: Percent
        }

        /// calculates an HP payment schedule and key statistics
        let calculate (p: Parameters) =
            let advance = p.VehiclePrice - p.Deposit
            let monthlyRate = Percent.toDecimal p.AnnualInterestRate / 12m
            let n = p.TermMonths

            // Fully-amortising annuity (no balloon): PMT = PV × r / (1 − (1+r)^−n)
            let monthlyPayment =
                if monthlyRate = 0m || n = 0 then
                    if n = 0 then 0L<Cent>
                    else Cent.toDecimal advance / decimal n |> Cent.fromDecimal
                else
                    let r = monthlyRate
                    let pv = Cent.toDecimal advance
                    let nthPower = powi n (1m + r)
                    let discountFactor = 1m / nthPower
                    pv * r / (1m - discountFactor)
                    |> Cent.fromDecimal

            let totalMonthlyPayments = monthlyPayment * int64 n
            let totalAmountPayable = p.Deposit + totalMonthlyPayments + p.OptionToPurchaseFee
            let totalCostOfCredit = totalAmountPayable - p.VehiclePrice

            // APR cash flows: n monthly payments; option-to-purchase fee on the final payment date
            let regularTransfers =
                [|
                    for i in 1 .. n do
                        yield {
                            Apr.TransferType = Apr.Payment
                            Apr.TransferDate = p.FirstPaymentDate.AddMonths(i - 1)
                            Apr.Value = monthlyPayment
                        }
                |]

            let aprTransfers =
                if p.OptionToPurchaseFee = 0L<Cent> then
                    regularTransfers
                else
                    [|
                        yield! regularTransfers
                        yield {
                            Apr.TransferType = Apr.Payment
                            Apr.TransferDate = p.FirstPaymentDate.AddMonths(n - 1)
                            Apr.Value = p.OptionToPurchaseFee
                        }
                    |]

            let aprSolution = Apr.calculate p.AprMethod advance p.AgreementDate aprTransfers
            let apr = Apr.toPercent p.AprMethod aprSolution

            {
                Advance = advance
                MonthlyPayment = monthlyPayment
                TotalMonthlyPayments = totalMonthlyPayments
                TotalAmountPayable = totalAmountPayable
                TotalCostOfCredit = totalCostOfCredit
                Apr = apr
            }

    // ============================================================
    // 3. Finance Lease
    // ============================================================

    /// Finance Lease calculations
    ///
    /// A rental schedule with a residual (balloon) value at the end. There is no automatic title transfer.
    /// Lease payments and the residual value are independently configurable.
    module FinanceLease =

        /// parameters for a Finance Lease agreement
        [<Struct>]
        type Parameters = {
            /// the date the agreement commences
            AgreementDate: Date
            /// the value of the asset being financed (e.g. the vehicle price)
            AssetValue: int64<Cent>
            /// any initial rental payment made at the start of the lease; reduces the financed amount
            InitialRental: int64<Cent>
            /// the residual value payable at the end of the lease term (the lessor's estimated residual)
            ResidualValue: int64<Cent>
            /// the nominal annual interest rate used to calculate regular rental payments
            AnnualInterestRate: Percent
            /// the number of regular monthly rental payments (excluding any initial rental)
            TermMonths: int
            /// the date on which the first regular monthly rental payment falls due
            FirstPaymentDate: Date
            /// the APR calculation method to apply
            AprMethod: Apr.CalculationMethod
        }

        /// the result of a Finance Lease calculation
        [<Struct>]
        type Result = {
            /// the financed amount: AssetValue − InitialRental
            FinancedAmount: int64<Cent>
            /// the level monthly rental payment
            MonthlyRental: int64<Cent>
            /// the residual value payable at end of the lease term
            ResidualPayment: int64<Cent>
            /// the total of all rentals: InitialRental + (MonthlyRental × TermMonths)
            TotalRentals: int64<Cent>
            /// the total amount payable: TotalRentals + ResidualValue
            TotalAmountPayable: int64<Cent>
            /// the APR calculated using the specified calculation method
            Apr: Percent
        }

        /// calculates a Finance Lease payment schedule and key statistics
        let calculate (p: Parameters) =
            let financed = p.AssetValue - p.InitialRental
            let monthlyRate = Percent.toDecimal p.AnnualInterestRate / 12m
            let n = p.TermMonths
            let rv = p.ResidualValue

            // Lease payments with residual: PMT = (PV − RV/(1+r)^n) × r / (1 − (1+r)^−n)
            let monthlyRental =
                if monthlyRate = 0m || n = 0 then
                    if n = 0 then 0L<Cent>
                    else
                        (Cent.toDecimal financed - Cent.toDecimal rv) / decimal n
                        |> Cent.fromDecimal
                else
                    let r = monthlyRate
                    let pv = Cent.toDecimal financed
                    let fv = Cent.toDecimal rv
                    let nthPower = powi n (1m + r)
                    let discountFactor = 1m / nthPower
                    (pv - fv * discountFactor) * r / (1m - discountFactor)
                    |> Cent.fromDecimal

            let totalRentals = p.InitialRental + monthlyRental * int64 n
            let totalAmountPayable = totalRentals + rv

            // APR cash flows: the financed amount on day 0; regular rentals; residual on final payment date
            let aprTransfers =
                [|
                    for i in 1 .. n do
                        yield {
                            Apr.TransferType = Apr.Payment
                            Apr.TransferDate = p.FirstPaymentDate.AddMonths(i - 1)
                            Apr.Value = monthlyRental
                        }
                    if rv > 0L<Cent> then
                        yield {
                            Apr.TransferType = Apr.Payment
                            Apr.TransferDate = p.FirstPaymentDate.AddMonths(n - 1)
                            Apr.Value = rv
                        }
                |]

            let aprSolution = Apr.calculate p.AprMethod financed p.AgreementDate aprTransfers
            let apr = Apr.toPercent p.AprMethod aprSolution

            {
                FinancedAmount = financed
                MonthlyRental = monthlyRental
                ResidualPayment = rv
                TotalRentals = totalRentals
                TotalAmountPayable = totalAmountPayable
                Apr = apr
            }

    // ============================================================
    // 4. Add-on Insurance (PPI / GAP)
    // ============================================================

    /// Add-on insurance cost analysis (PPI / debt protection / GAP)
    ///
    /// Expresses the implied APR uplift when a Payment Protection Insurance (PPI) or
    /// Guaranteed Asset Protection (GAP) premium is bundled into the finance advance,
    /// making the true cost of the finance transparent to the borrower.
    module AddOnInsurance =

        /// parameters for add-on insurance cost analysis
        [<Struct>]
        type Parameters = {
            /// the date the agreement commences
            AgreementDate: Date
            /// the base finance advance, excluding any insurance premium
            Advance: int64<Cent>
            /// the total insurance premium added to the finance amount (e.g. a single GAP premium)
            InsurancePremium: int64<Cent>
            /// the nominal annual interest rate of the underlying finance agreement
            AnnualInterestRate: Percent
            /// the number of monthly payments
            TermMonths: int
            /// the date on which the first monthly payment falls due
            FirstPaymentDate: Date
            /// the APR calculation method to apply
            AprMethod: Apr.CalculationMethod
        }

        /// the result of an add-on insurance cost analysis
        [<Struct>]
        type Result = {
            /// the monthly payment on the base loan (without insurance)
            MonthlyPaymentWithoutInsurance: int64<Cent>
            /// the total advance including the insurance premium
            TotalAdvance: int64<Cent>
            /// the monthly payment on the combined loan (base + insurance premium)
            MonthlyPaymentWithInsurance: int64<Cent>
            /// the APR on the base loan, without any insurance
            BaseApr: Percent
            /// the true APR on the combined loan including the insurance premium
            TrueApr: Percent
            /// the APR uplift attributable to the insurance premium (TrueApr − BaseApr)
            AprUplift: Percent
        }

        /// calculates the implied APR uplift from a bundled insurance premium
        ///
        /// The APR uplift is calculated by comparing:
        ///   - the base APR: advance vs. payments on the base advance alone
        ///   - the true APR: the same advance (what the customer actually receives) vs. the
        ///     higher payments required to service the advance plus insurance premium
        ///
        /// This reveals the true effective cost of the bundled insurance, because the customer
        /// only receives value equal to the base advance but services a larger debt.
        let calculate (p: Parameters) =
            let n = p.TermMonths
            let monthlyRate = Percent.toDecimal p.AnnualInterestRate / 12m
            let totalAdvance = p.Advance + p.InsurancePremium

            // compute monthly payments using standard PMT: PV × r / (1 − (1+r)^−n)
            let computeMonthlyPayment (principal: int64<Cent>) =
                if monthlyRate = 0m || n = 0 then
                    if n = 0 then 0L<Cent>
                    else Cent.toDecimal principal / decimal n |> Cent.fromDecimal
                else
                    let r = monthlyRate
                    let pv = Cent.toDecimal principal
                    let nthPower = powi n (1m + r)
                    let discountFactor = 1m / nthPower
                    pv * r / (1m - discountFactor)
                    |> Cent.fromDecimal

            let paymentWithout = computeMonthlyPayment p.Advance
            let paymentWith = computeMonthlyPayment totalAdvance

            let makeTransfers paymentValue =
                [|
                    for i in 1 .. n do
                        yield {
                            Apr.TransferType = Apr.Payment
                            Apr.TransferDate = p.FirstPaymentDate.AddMonths(i - 1)
                            Apr.Value = paymentValue
                        }
                |]

            // Base APR: what the customer receives (Advance) vs. payments without insurance
            let baseAprSolution = Apr.calculate p.AprMethod p.Advance p.AgreementDate (makeTransfers paymentWithout)
            let baseApr = Apr.toPercent p.AprMethod baseAprSolution

            // True APR: what the customer receives (Advance) vs. insurance-inflated payments
            // This reveals the true cost because the customer only received Advance in value
            // but must service total advance (Advance + InsurancePremium) worth of payments
            let trueAprSolution = Apr.calculate p.AprMethod p.Advance p.AgreementDate (makeTransfers paymentWith)
            let trueApr = Apr.toPercent p.AprMethod trueAprSolution

            let (Percent baseRate) = baseApr
            let (Percent trueRate) = trueApr

            {
                MonthlyPaymentWithoutInsurance = paymentWithout
                TotalAdvance = totalAdvance
                MonthlyPaymentWithInsurance = paymentWith
                BaseApr = baseApr
                TrueApr = trueApr
                AprUplift = Percent(trueRate - baseRate)
            }
