namespace FSharp.Finance.Personal.Tests

open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal
open Calculation
open StudentLoan

module StudentLoanTests =

    // ──────────────────────────────────────────────────────────────────
    // annualRepayment
    // ──────────────────────────────────────────────────────────────────

    module AnnualRepaymentTests =

        let plan2Rate = { Rate = Percent 9m }

        [<Fact>]
        let ``annualRepayment returns 0 when income equals threshold`` () =
            let threshold = 2729_500L<Cent> // £27,295
            let income    = 2729_500L<Cent>
            let actual    = annualRepayment income threshold plan2Rate
            actual |> should equal 0L<Cent>

        [<Fact>]
        let ``annualRepayment returns 0 when income is below threshold`` () =
            let threshold = 2729_500L<Cent>
            let income    = 2000_000L<Cent>
            let actual    = annualRepayment income threshold plan2Rate
            actual |> should equal 0L<Cent>

        [<Fact>]
        let ``annualRepayment calculates 9% of income above Plan 2 threshold`` () =
            // £30,000 income, £27,295 threshold → 9% of £2,705 = £243.45 → 24345 cents
            let threshold = 2729_500L<Cent>
            let income    = 3000_000L<Cent>
            let actual    = annualRepayment income threshold plan2Rate
            actual |> should equal 24345L<Cent>

        [<Fact>]
        let ``annualRepayment calculates 9% of income above Plan 1 threshold`` () =
            // £25,000 income, £22,015 Plan 1 threshold → 9% of £2,985 = £268.65 → 26865 cents
            let threshold = 2201_500L<Cent>
            let income    = 2500_000L<Cent>
            let actual    = annualRepayment income threshold { Rate = Percent 9m }
            actual |> should equal 26865L<Cent>

        [<Fact>]
        let ``annualRepayment for US IBR uses 10% rate`` () =
            // £40,000 income, £20,000 threshold → 10% of £20,000 = £2,000 → 200000 cents
            let threshold = 2000_000L<Cent>
            let income    = 4000_000L<Cent>
            let actual    = annualRepayment income threshold { Rate = Percent 10m }
            actual |> should equal 200000L<Cent>

    // ──────────────────────────────────────────────────────────────────
    // annualInterestRate
    // ──────────────────────────────────────────────────────────────────

    module AnnualInterestRateTests =

        [<Fact>]
        let ``annualInterestRate for InterestMethod.NoInterest returns 0%`` () =
            let actual = annualInterestRate 3000_000L<Cent> InterestMethod.NoInterest
            actual |> should equal (Percent 0m)

        [<Fact>]
        let ``annualInterestRate for RpiOnly returns the RPI rate`` () =
            let actual = annualInterestRate 3000_000L<Cent> (InterestMethod.RpiOnly(Percent 3.1m))
            actual |> should equal (Percent 3.1m)

        [<Fact>]
        let ``annualInterestRate for RpiPlusScaledMargin at or below lower income returns RPI only`` () =
            // Plan 2: income at or below £27,295 → just RPI
            let lower  = 2729_500L<Cent>
            let upper  = 4950_000L<Cent>
            let actual = annualInterestRate lower (InterestMethod.RpiPlusScaledMargin(Percent 3.1m, Percent 3m, lower, upper))
            actual |> should equal (Percent 3.1m)

        [<Fact>]
        let ``annualInterestRate for RpiPlusScaledMargin at or above upper income returns RPI + maxMargin`` () =
            let lower  = 2729_500L<Cent>
            let upper  = 4950_000L<Cent>
            let actual = annualInterestRate upper (InterestMethod.RpiPlusScaledMargin(Percent 3.1m, Percent 3m, lower, upper))
            actual |> should equal (Percent 6.1m)

        [<Fact>]
        let ``annualInterestRate for RpiPlusScaledMargin at midpoint returns RPI + half maxMargin`` () =
            let lower  = 2729_500L<Cent>
            let upper  = 4950_000L<Cent>
            let mid    = (lower + upper) / 2L
            let actual = annualInterestRate mid (InterestMethod.RpiPlusScaledMargin(Percent 3.1m, Percent 3m, lower, upper))
            // 3.1 + 1.5 = 4.6
            let (Percent rate) = actual
            rate |> should (equalWithin 0.001m) 4.6m

    // ──────────────────────────────────────────────────────────────────
    // projectBalance
    // ──────────────────────────────────────────────────────────────────

    module ProjectBalanceTests =

        [<Fact>]
        let ``projectBalance returns a projection with one entry per year up to write-off`` () =
            let projection =
                projectBalance
                    5000_000L<Cent>  // £50,000 initial balance
                    3000_000L<Cent>  // £30,000 income
                    2729_500L<Cent>  // £27,295 threshold (Plan 2)
                    (Percent 0m)     // no income growth
                    ThresholdUprating.Fixed
                    { Rate = Percent 9m }
                    InterestMethod.NoInterest
                    { AfterYears = 30 }

            // With no interest, constant income of £30k, Plan 2 threshold £27,295:
            // repayment = 9% of (30000 - 27295) = 9% × 2705 = £243.45/yr = 24345¢
            // £50,000 / £243.45 ≈ 205 years to clear → write-off after 30 years
            projection |> Array.length |> should equal 30

        [<Fact>]
        let ``projectBalance balance decreases each year when there is no interest and income exceeds threshold`` () =
            let projection =
                projectBalance
                    1000_000L<Cent>  // £10,000
                    3000_000L<Cent>  // £30,000 income
                    2729_500L<Cent>  // Plan 2 threshold
                    (Percent 0m)
                    ThresholdUprating.Fixed
                    { Rate = Percent 9m }
                    InterestMethod.NoInterest
                    { AfterYears = 30 }

            projection
            |> Array.pairwise
            |> Array.forall (fun (prev, next) -> next.Balance <= prev.Balance)
            |> should equal true

        [<Fact>]
        let ``projectBalance stops early when balance reaches zero`` () =
            // High income, small balance — should clear quickly
            let projection =
                projectBalance
                    100_000L<Cent>   // £1,000 balance
                    6000_000L<Cent>  // £60,000 income
                    2729_500L<Cent>  // Plan 2 threshold
                    (Percent 0m)
                    ThresholdUprating.Fixed
                    { Rate = Percent 9m }
                    InterestMethod.NoInterest
                    { AfterYears = 30 }

            // repayment = 9% of (60000 - 27295) = 9% × 32705 = £2,943.45/yr = 294345¢ > 100000¢ → clears in year 1
            projection |> Array.length |> should equal 1
            projection[0].Balance |> should equal 0L<Cent>

        [<Fact>]
        let ``projectBalance first year balance equals initialBalance minus repayment when no interest`` () =
            let initial   = 5000_000L<Cent>
            let income    = 3500_000L<Cent>
            let threshold = 2729_500L<Cent>
            let repayment = annualRepayment income threshold { Rate = Percent 9m }

            let projection =
                projectBalance
                    initial
                    income
                    threshold
                    (Percent 0m)
                    ThresholdUprating.Fixed
                    { Rate = Percent 9m }
                    InterestMethod.NoInterest
                    { AfterYears = 30 }

            projection[0].Balance |> should equal (initial - repayment)

    // ──────────────────────────────────────────────────────────────────
    // estimatedWriteOff
    // ──────────────────────────────────────────────────────────────────

    module EstimatedWriteOffTests =

        [<Fact>]
        let ``estimatedWriteOff returns FullyRepaid when balance clears before write-off term`` () =
            let result =
                estimatedWriteOff
                    100_000L<Cent>   // £1,000 balance
                    6000_000L<Cent>  // £60,000 income
                    2729_500L<Cent>  // Plan 2 threshold
                    (Percent 0m)
                    ThresholdUprating.Fixed
                    { Rate = Percent 9m }
                    InterestMethod.NoInterest
                    { AfterYears = 30 }

            (match result with WriteOffEstimate.FullyRepaid _ -> true | _ -> false)
            |> should be True

        [<Fact>]
        let ``estimatedWriteOff returns PartialWriteOff when balance does not clear before write-off term`` () =
            let result =
                estimatedWriteOff
                    5000_000L<Cent>  // £50,000 balance
                    3000_000L<Cent>  // £30,000 income
                    2729_500L<Cent>  // Plan 2 threshold
                    (Percent 0m)
                    ThresholdUprating.Fixed
                    { Rate = Percent 9m }
                    InterestMethod.NoInterest
                    { AfterYears = 30 }

            (match result with WriteOffEstimate.PartialWriteOff _ -> true | _ -> false)
            |> should be True

        [<Fact>]
        let ``estimatedWriteOff FullyRepaid year matches projection`` () =
            let result =
                estimatedWriteOff
                    100_000L<Cent>
                    6000_000L<Cent>
                    2729_500L<Cent>
                    (Percent 0m)
                    ThresholdUprating.Fixed
                    { Rate = Percent 9m }
                    InterestMethod.NoInterest
                    { AfterYears = 30 }

            (match result with WriteOffEstimate.FullyRepaid _ -> true | _ -> false)
            |> should be True
            match result with
            | WriteOffEstimate.FullyRepaid years -> years |> should equal 1
            | _ -> ()

        [<Fact>]
        let ``planDefaults for UkPlan2 returns 9% rate, 30-year write-off, and RPI plus scaled margin`` () =
            let (repRate, writeOff, interestMethod) = planDefaults Plan.UkPlan2 (Percent 3.1m)
            repRate   |> should equal { Rate = Percent 9m }
            writeOff  |> should equal { AfterYears = 30 }
            (match interestMethod with InterestMethod.RpiPlusScaledMargin _ -> true | _ -> false)
            |> should be True

        [<Fact>]
        let ``planDefaults for UkPlan1 returns 9% rate, 25-year write-off, and RpiOnly`` () =
            let (repRate, writeOff, interestMethod) = planDefaults Plan.UkPlan1 (Percent 3.1m)
            repRate   |> should equal { Rate = Percent 9m }
            writeOff  |> should equal { AfterYears = 25 }
            (match interestMethod with InterestMethod.RpiOnly _ -> true | _ -> false)
            |> should be True

        [<Fact>]
        let ``planDefaults for UsIbr returns 10% rate, 20-year write-off, and no interest`` () =
            let (repRate, writeOff, interestMethod) = planDefaults Plan.UsIbr (Percent 0m)
            repRate   |> should equal { Rate = Percent 10m }
            writeOff  |> should equal { AfterYears = 20 }
            (match interestMethod with InterestMethod.NoInterest -> true | _ -> false)
            |> should be True

    // ──────────────────────────────────────────────────────────────────
    // uprateThreshold
    // ──────────────────────────────────────────────────────────────────

    module UprateThresholdTests =

        [<Fact>]
        let ``uprateThreshold Fixed returns the same threshold`` () =
            let threshold = 2729_500L<Cent>
            let actual    = uprateThreshold threshold ThresholdUprating.Fixed
            actual |> should equal threshold

        [<Fact>]
        let ``uprateThreshold Rpi 3% increases threshold by 3%`` () =
            // £27,295 * 1.03 = £28,113.85 → 2811385¢
            let threshold = 2729_500L<Cent>
            let actual    = uprateThreshold threshold (ThresholdUprating.Rpi(Percent 3m))
            actual |> should equal 2811385L<Cent>

        [<Fact>]
        let ``uprateThreshold GovernmentPolicy 5% increases threshold by 5%`` () =
            let threshold = 2000_000L<Cent>
            let actual    = uprateThreshold threshold (ThresholdUprating.GovernmentPolicy(Percent 5m))
            actual |> should equal 2100_000L<Cent>
