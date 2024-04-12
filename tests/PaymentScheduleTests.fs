namespace FSharp.Finance.Personal.Tests

open System
open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal

module PaymentScheduleTests =

    open ArrayExtension
    open Calculation
    open Currency
    open CustomerPayments
    open DateDay
    open FeesAndCharges
    open PaymentSchedule
    open Percentages
    open ValueOptionCE

    module Biweekly =
        let biweeklyParameters principal offset =
            let startDate = Date(2023, 11, 15)
            {
                AsOfDate = startDate
                ScheduleType = ScheduleType.Original
                StartDate = startDate
                Principal = principal
                PaymentSchedule = RegularSchedule (
                    UnitPeriodConfig = UnitPeriod.Weekly(2, startDate.AddDays(int offset)),
                    PaymentCount = 11
                )
                FeesAndCharges = {
                    Fees = [| Fee.FacilitationFee (Amount.Percentage (Percent 189.47m, ValueNone, ValueSome RoundDown)) |]
                    FeesAmortisation = Fees.FeeAmortisation.AmortiseProportionately
                    FeesSettlementRefund = Fees.SettlementRefund.ProRata
                    Charges = [| Charge.InsufficientFunds (Amount.Simple 7_50L<Cent>); Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                    ChargesHolidays = [||]
                    ChargesGrouping = OneChargeTypePerDay
                    LatePaymentGracePeriod = 0<DurationDay>
                }
                Interest = {
                    StandardRate = Interest.Rate.Annual (Percent 9.95m)
                    Cap = Interest.Cap.none
                    InitialGracePeriod = 3<DurationDay>
                    PromotionalRates = [||]
                    RateOnNegativeBalance = ValueNone
                }
                Calculation = {
                    AprMethod = Apr.CalculationMethod.UsActuarial 8
                    RoundingOptions = RoundingOptions.recommended
                    MinimumPayment = DeferOrWriteOff 50L<Cent>
                    PaymentTimeout = 3<DurationDay>
                }
            }

        [<Fact>]
        let ``$1200 with short first period`` () =
            let actual = biweeklyParameters 1200_00L<Cent> 8<DurationDay> |> calculate BelowZero
            actual |> ValueOption.iter(_.Items >> Formatting.outputListToHtml "out/PaymentScheduleTest.Biweekly001.md")
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 148<OffsetDay>
                LevelPayment = 322_53L<Cent>
                FinalPayment = 322_53L<Cent>
                PaymentTotal = 3547_83L<Cent>
                PrincipalTotal = 3473_64L<Cent>
                InterestTotal = 74_19L<Cent>
                Apr = fst actual.Value.Apr, ValueSome(Percent 717.412507m)
                CostToBorrowingRatio = Percent 67.59m
            }
            actual |> should equal expected

        [<Fact>]
        let ``2) $1200 with first period equal to unit-period length`` () =
            let actual = biweeklyParameters 1200_00L<Cent> 14<DurationDay> |> calculate BelowZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/PaymentScheduleTest.Biweekly002.md")
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 154<OffsetDay>
                LevelPayment = 323_06L<Cent>
                FinalPayment = 323_03L<Cent>
                PaymentTotal = 3553_63L<Cent>
                PrincipalTotal = 3473_64L<Cent>
                InterestTotal = 79_99L<Cent>
                Apr = fst actual.Value.Apr, ValueSome(Percent 637.159359m)
                CostToBorrowingRatio = Percent 67.76m
            }
            actual |> should equal expected

        [<Fact>]
        let ``3) $1200 with long first period`` () =
            let actual = biweeklyParameters 1200_00L<Cent> 15<DurationDay> |> calculate BelowZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/PaymentScheduleTest.Biweekly003.md")
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 155<OffsetDay>
                LevelPayment = 323_15L<Cent>
                FinalPayment = 323_10L<Cent>
                PaymentTotal = 3554_60L<Cent>
                PrincipalTotal = 3473_64L<Cent>
                InterestTotal = 80_96L<Cent>
                Apr = fst actual.Value.Apr, ValueSome(Percent 623.703586m)
                CostToBorrowingRatio = Percent 67.78m
            }
            actual |> should equal expected

    module Monthly =

        let monthlyParameters principal offset paymentCount =
            let startDate = Date(2023, 12, 07)
            {
                AsOfDate = startDate
                ScheduleType = ScheduleType.Original
                StartDate = startDate
                Principal = principal
                PaymentSchedule = RegularSchedule (
                    UnitPeriodConfig = (startDate.AddDays(int offset) |> fun d -> UnitPeriod.Monthly(1, d.Year, d.Month, d.Day * 1)),
                    PaymentCount = paymentCount
                )
                FeesAndCharges = {
                    Fees = [||]
                    FeesAmortisation = Fees.FeeAmortisation.AmortiseProportionately
                    FeesSettlementRefund = Fees.SettlementRefund.ProRata
                    Charges = [||]
                    ChargesHolidays = [||]
                    ChargesGrouping = OneChargeTypePerDay
                    LatePaymentGracePeriod = 0<DurationDay>
                }
                Interest = {
                    StandardRate = Interest.Rate.Daily (Percent 0.798m)
                    Cap = Interest.Cap.example
                    InitialGracePeriod = 3<DurationDay>
                    PromotionalRates = [||]
                    RateOnNegativeBalance = ValueNone
                }
                Calculation = {
                    AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                    RoundingOptions = { ChargesRounding = RoundDown; FeesRounding = RoundDown; InterestRounding = Round MidpointRounding.AwayFromZero; PaymentRounding = Round MidpointRounding.AwayFromZero }
                    MinimumPayment = DeferOrWriteOff 50L<Cent>
                    PaymentTimeout = 3<DurationDay>
                }
            }

        [<Fact>]
        let ``£0100 with 04 days to first payment and 5 repayments`` () =
            let actual = monthlyParameters 100_00L<Cent> 4<DurationDay> 5 |> calculate AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/PaymentScheduleTest.Monthly001.md")
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 126<OffsetDay>
                LevelPayment = 30_49L<Cent>
                FinalPayment = 30_46L<Cent>
                PaymentTotal = 152_42L<Cent>
                PrincipalTotal = 100_00L<Cent>
                InterestTotal = 52_42L<Cent>
                Apr = fst actual.Value.Apr, ValueSome(Percent 1280.8m)
                CostToBorrowingRatio = Percent 52.42m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£0100 with 08 days to first payment and 5 repayments`` () =
            let actual = monthlyParameters 100_00L<Cent> 8<DurationDay> 5 |> calculate AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/PaymentScheduleTest.Monthly009.md")
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 130<OffsetDay>
                LevelPayment = 31_43L<Cent>
                FinalPayment = 31_42L<Cent>
                PaymentTotal = 157_14L<Cent>
                PrincipalTotal = 100_00L<Cent>
                InterestTotal = 57_14L<Cent>
                Apr = fst actual.Value.Apr, ValueSome(Percent 1295.9m)
                CostToBorrowingRatio = Percent 57.14m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£0100 with 12 days to first payment and 4 repayments`` () =
            let actual = monthlyParameters 100_00L<Cent> 12<DurationDay> 4 |> calculate AboveZero //AroundZero finds negative principal balance first
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/PaymentScheduleTest.Monthly017.md")
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 103<OffsetDay>
                LevelPayment = 36_94L<Cent>
                FinalPayment = 36_95L<Cent>
                PaymentTotal = 147_77L<Cent>
                PrincipalTotal = 100_00L<Cent>
                InterestTotal = 47_77L<Cent>
                Apr = fst actual.Value.Apr, ValueSome(Percent 1312.3m)
                CostToBorrowingRatio = Percent 47.77m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£0100 with 16 days to first payment and 4 repayments`` () =
            let actual = monthlyParameters 100_00L<Cent> 16<DurationDay> 4 |> calculate BelowZero //AroundZero finds positive principal balance first
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/PaymentScheduleTest.Monthly025.md")
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 107<OffsetDay>
                LevelPayment = 38_02L<Cent>
                FinalPayment = 38_00L<Cent>
                PaymentTotal = 152_06L<Cent>
                PrincipalTotal = 100_00L<Cent>
                InterestTotal = 52_06L<Cent>
                Apr = fst actual.Value.Apr, ValueSome(Percent 1309m)
                CostToBorrowingRatio = Percent 52.06m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£0100 with 20 days to first payment and 4 repayments`` () =
            let actual = monthlyParameters 100_00L<Cent> 20<DurationDay> 4 |> calculate AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/PaymentScheduleTest.Monthly033.md")
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 111<OffsetDay>
                LevelPayment = 39_09L<Cent>
                FinalPayment = 39_11L<Cent>
                PaymentTotal = 156_38L<Cent>
                PrincipalTotal = 100_00L<Cent>
                InterestTotal = 56_38L<Cent>
                Apr = fst actual.Value.Apr, ValueSome(Percent 1299.7m)
                CostToBorrowingRatio = Percent 56.38m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£0100 with 24 days to first payment and 4 repayments`` () =
            let actual = monthlyParameters 100_00L<Cent> 24<DurationDay> 4 |> calculate BelowZero //AroundZero finds positive principal balance first
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/PaymentScheduleTest.Monthly041.md")
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 115<OffsetDay>
                LevelPayment = 40_06L<Cent>
                FinalPayment = 40_04L<Cent>
                PaymentTotal = 160_22L<Cent>
                PrincipalTotal = 100_00L<Cent>
                InterestTotal = 60_22L<Cent>
                Apr = fst actual.Value.Apr, ValueSome(Percent 1287.7m)
                CostToBorrowingRatio = Percent 60.22m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£0100 with 28 days to first payment and 4 repayments`` () =
            let actual = monthlyParameters 100_00L<Cent> 28<DurationDay> 4 |> calculate BelowZero //AroundZero finds positive principal balance first
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/PaymentScheduleTest.Monthly049.md")
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 119<OffsetDay>
                LevelPayment = 41_13L<Cent>
                FinalPayment = 41_11L<Cent>
                PaymentTotal = 164_50L<Cent>
                PrincipalTotal = 100_00L<Cent>
                InterestTotal = 64_50L<Cent>
                Apr = fst actual.Value.Apr, ValueSome(Percent 1268.8m)
                CostToBorrowingRatio = Percent 64.5m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£0100 with 32 days to first payment and 4 repayments`` () =
            let actual = monthlyParameters 100_00L<Cent> 32<DurationDay> 4 |> calculate AboveZero //AroundZero finds negative principal balance first
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/PaymentScheduleTest.Monthly057.md")
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 123<OffsetDay>
                LevelPayment = 42_20L<Cent>
                FinalPayment = 42_22L<Cent>
                PaymentTotal = 168_82L<Cent>
                PrincipalTotal = 100_00L<Cent>
                InterestTotal = 68_82L<Cent>
                Apr = fst actual.Value.Apr, ValueSome(Percent 1248.6m)
                CostToBorrowingRatio = Percent 68.82m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£0300 with 04 days to first payment and 5 repayments`` () =
            let actual = monthlyParameters 300_00L<Cent> 4<DurationDay> 5 |> calculate AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/PaymentScheduleTest.Monthly002.md")
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 126<OffsetDay>
                LevelPayment = 91_46L<Cent>
                FinalPayment = 91_50L<Cent>
                PaymentTotal = 457_34L<Cent>
                PrincipalTotal = 300_00L<Cent>
                InterestTotal = 157_34L<Cent>
                Apr = fst actual.Value.Apr, ValueSome(Percent 1281.4m)
                CostToBorrowingRatio = Percent 52.45m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£0300 with 08 days to first payment and 5 repayments`` () =
            let actual = monthlyParameters 300_00L<Cent> 8<DurationDay> 5 |> calculate AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/PaymentScheduleTest.Monthly010.md")
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 130<OffsetDay>
                LevelPayment = 94_29L<Cent>
                FinalPayment = 94_31L<Cent>
                PaymentTotal = 471_47L<Cent>
                PrincipalTotal = 300_00L<Cent>
                InterestTotal = 171_47L<Cent>
                Apr = fst actual.Value.Apr, ValueSome(Percent 1296.5m)
                CostToBorrowingRatio = Percent 57.16m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£0300 with 12 days to first payment and 4 repayments`` () =
            let actual = monthlyParameters 300_00L<Cent> 12<DurationDay> 4 |> calculate AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/PaymentScheduleTest.Monthly018.md")
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 103<OffsetDay>
                LevelPayment = 110_82L<Cent>
                FinalPayment = 110_84L<Cent>
                PaymentTotal = 443_30L<Cent>
                PrincipalTotal = 300_00L<Cent>
                InterestTotal = 143_30L<Cent>
                Apr = fst actual.Value.Apr, ValueSome(Percent 1312.1m)
                CostToBorrowingRatio = Percent 47.77m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£0300 with 16 days to first payment and 4 repayments`` () =
            let actual = monthlyParameters 300_00L<Cent> 16<DurationDay> 4 |> calculate AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/PaymentScheduleTest.Monthly026.md")
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 107<OffsetDay>
                LevelPayment = 114_05L<Cent>
                FinalPayment = 114_03L<Cent>
                PaymentTotal = 456_18L<Cent>
                PrincipalTotal = 300_00L<Cent>
                InterestTotal = 156_18L<Cent>
                Apr = fst actual.Value.Apr, ValueSome(Percent 1308.8m)
                CostToBorrowingRatio = Percent 52.06m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£0300 with 20 days to first payment and 4 repayments`` () =
            let actual = monthlyParameters 300_00L<Cent> 20<DurationDay> 4 |> calculate AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/PaymentScheduleTest.Monthly034.md")
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 111<OffsetDay>
                LevelPayment = 117_28L<Cent>
                FinalPayment = 117_28L<Cent>
                PaymentTotal = 469_12L<Cent>
                PrincipalTotal = 300_00L<Cent>
                InterestTotal = 169_12L<Cent>
                Apr = fst actual.Value.Apr, ValueSome(Percent 1299.6m)
                CostToBorrowingRatio = Percent 56.37m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£0300 with 24 days to first payment and 4 repayments`` () =
            let actual = monthlyParameters 300_00L<Cent> 24<DurationDay> 4 |> calculate AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/PaymentScheduleTest.Monthly042.md")
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 115<OffsetDay>
                LevelPayment = 120_17L<Cent>
                FinalPayment = 120_17L<Cent>
                PaymentTotal = 480_68L<Cent>
                PrincipalTotal = 300_00L<Cent>
                InterestTotal = 180_68L<Cent>
                Apr = fst actual.Value.Apr, ValueSome(Percent 1287.8m)
                CostToBorrowingRatio = Percent 60.23m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£0300 with 28 days to first payment and 4 repayments`` () =
            let actual = monthlyParameters 300_00L<Cent> 28<DurationDay> 4 |> calculate BelowZero //AroundZero finds positive principal balance first
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/PaymentScheduleTest.Monthly050.md")
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 119<OffsetDay>
                LevelPayment = 123_39L<Cent>
                FinalPayment = 123_38L<Cent>
                PaymentTotal = 493_55L<Cent>
                PrincipalTotal = 300_00L<Cent>
                InterestTotal = 193_55L<Cent>
                Apr = fst actual.Value.Apr, ValueSome(Percent 1269.3m)
                CostToBorrowingRatio = Percent 64.52m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£0300 with 32 days to first payment and 4 repayments`` () =
            let actual = monthlyParameters 300_00L<Cent> 32<DurationDay> 4 |> calculate AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/PaymentScheduleTest.Monthly058.md")
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 123<OffsetDay>
                LevelPayment = 126_61L<Cent>
                FinalPayment = 126_61L<Cent>
                PaymentTotal = 506_44L<Cent>
                PrincipalTotal = 300_00L<Cent>
                InterestTotal = 206_44L<Cent>
                Apr = fst actual.Value.Apr, ValueSome(Percent 1248.6m)
                CostToBorrowingRatio = Percent 68.81m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£0500 with 04 days to first payment and 5 repayments`` () =
            let actual = monthlyParameters 500_00L<Cent> 4<DurationDay> 5 |> calculate AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/PaymentScheduleTest.Monthly003.md")
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 126<OffsetDay>
                LevelPayment = 152_44L<Cent>
                FinalPayment = 152_43L<Cent>
                PaymentTotal = 762_19L<Cent>
                PrincipalTotal = 500_00L<Cent>
                InterestTotal = 262_19L<Cent>
                Apr = fst actual.Value.Apr, ValueSome(Percent 1281.2m)
                CostToBorrowingRatio = Percent 52.44m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£0500 with 08 days to first payment and 5 repayments`` () =
            let actual = monthlyParameters 500_00L<Cent> 8<DurationDay> 5 |> calculate AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/PaymentScheduleTest.Monthly011.md")
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 130<OffsetDay>
                LevelPayment = 157_15L<Cent>
                FinalPayment = 157_19L<Cent>
                PaymentTotal = 785_79L<Cent>
                PrincipalTotal = 500_00L<Cent>
                InterestTotal = 285_79L<Cent>
                Apr = fst actual.Value.Apr, ValueSome(Percent 1296.6m)
                CostToBorrowingRatio = Percent 57.16m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£0500 with 12 days to first payment and 4 repayments`` () =
            let actual = monthlyParameters 500_00L<Cent> 12<DurationDay> 4 |> calculate AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/PaymentScheduleTest.Monthly019.md")
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 103<OffsetDay>
                LevelPayment = 184_70L<Cent>
                FinalPayment = 184_71L<Cent>
                PaymentTotal = 738_81L<Cent>
                PrincipalTotal = 500_00L<Cent>
                InterestTotal = 238_81L<Cent>
                Apr = fst actual.Value.Apr, ValueSome(Percent 1311.9m)
                CostToBorrowingRatio = Percent 47.76m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£0500 with 16 days to first payment and 4 repayments`` () =
            let actual = monthlyParameters 500_00L<Cent> 16<DurationDay> 4 |> calculate AboveZero //AroundZero finds negative principal balance first
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/PaymentScheduleTest.Monthly027.md")
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 107<OffsetDay>
                LevelPayment = 190_08L<Cent>
                FinalPayment = 190_09L<Cent>
                PaymentTotal = 760_33L<Cent>
                PrincipalTotal = 500_00L<Cent>
                InterestTotal = 260_33L<Cent>
                Apr = fst actual.Value.Apr, ValueSome(Percent 1309m)
                CostToBorrowingRatio = Percent 52.07m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£0500 with 20 days to first payment and 4 repayments`` () =
            let actual = monthlyParameters 500_00L<Cent> 20<DurationDay> 4 |> calculate AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/PaymentScheduleTest.Monthly035.md")
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 111<OffsetDay>
                LevelPayment = 195_46L<Cent>
                FinalPayment = 195_49L<Cent>
                PaymentTotal = 781_87L<Cent>
                PrincipalTotal = 500_00L<Cent>
                InterestTotal = 281_87L<Cent>
                Apr = fst actual.Value.Apr, ValueSome(Percent 1299.6m)
                CostToBorrowingRatio = Percent 56.37m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£0500 with 24 days to first payment and 4 repayments`` () =
            let actual = monthlyParameters 500_00L<Cent> 24<DurationDay> 4 |> calculate AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/PaymentScheduleTest.Monthly043.md")
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 115<OffsetDay>
                LevelPayment = 200_28L<Cent>
                FinalPayment = 200_28L<Cent>
                PaymentTotal = 801_12L<Cent>
                PrincipalTotal = 500_00L<Cent>
                InterestTotal = 301_12L<Cent>
                Apr = fst actual.Value.Apr, ValueSome(Percent 1287.6m)
                CostToBorrowingRatio = Percent 60.22m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£0500 with 28 days to first payment and 4 repayments`` () =
            let actual = monthlyParameters 500_00L<Cent> 28<DurationDay> 4 |> calculate BelowZero //AroundZero finds positive principal balance first
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/PaymentScheduleTest.Monthly051.md")
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 119<OffsetDay>
                LevelPayment = 205_65L<Cent>
                FinalPayment = 205_63L<Cent>
                PaymentTotal = 822_58L<Cent>
                PrincipalTotal = 500_00L<Cent>
                InterestTotal = 322_58L<Cent>
                Apr = fst actual.Value.Apr, ValueSome(Percent 1269.3m)
                CostToBorrowingRatio = Percent 64.52m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£0500 with 32 days to first payment and 4 repayments`` () =
            let actual = monthlyParameters 500_00L<Cent> 32<DurationDay> 4 |> calculate AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/PaymentScheduleTest.Monthly059.md")
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 123<OffsetDay>
                LevelPayment = 211_01L<Cent>
                FinalPayment = 211_03L<Cent>
                PaymentTotal = 844_06L<Cent>
                PrincipalTotal = 500_00L<Cent>
                InterestTotal = 344_06L<Cent>
                Apr = fst actual.Value.Apr, ValueSome(Percent 1248.5m)
                CostToBorrowingRatio = Percent 68.81m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£0700 with 04 days to first payment and 5 repayments`` () =
            let actual = monthlyParameters 700_00L<Cent> 4<DurationDay> 5 |> calculate BelowZero //AroundZero finds positive principal balance first
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/PaymentScheduleTest.Monthly004.md")
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 126<OffsetDay>
                LevelPayment = 213_42L<Cent>
                FinalPayment = 213_39L<Cent>
                PaymentTotal = 1067_07L<Cent>
                PrincipalTotal = 700_00L<Cent>
                InterestTotal = 367_07L<Cent>
                Apr = fst actual.Value.Apr, ValueSome(Percent 1281.3m)
                CostToBorrowingRatio = Percent 52.44m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£0700 with 08 days to first payment and 5 repayments`` () =
            let actual = monthlyParameters 700_00L<Cent> 8<DurationDay> 5 |> calculate AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/PaymentScheduleTest.Monthly012.md")
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 130<OffsetDay>
                LevelPayment = 220_02L<Cent>
                FinalPayment = 219_99L<Cent>
                PaymentTotal = 1100_07L<Cent>
                PrincipalTotal = 700_00L<Cent>
                InterestTotal = 400_07L<Cent>
                Apr = fst actual.Value.Apr, ValueSome(Percent 1296.5m)
                CostToBorrowingRatio = Percent 57.15m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£0700 with 12 days to first payment and 4 repayments`` () =
            let actual = monthlyParameters 700_00L<Cent> 12<DurationDay> 4 |> calculate AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/PaymentScheduleTest.Monthly020.md")
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 103<OffsetDay>
                LevelPayment = 258_58L<Cent>
                FinalPayment = 258_60L<Cent>
                PaymentTotal = 1034_34L<Cent>
                PrincipalTotal = 700_00L<Cent>
                InterestTotal = 334_34L<Cent>
                Apr = fst actual.Value.Apr, ValueSome(Percent 1311.9m)
                CostToBorrowingRatio = Percent 47.76m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£0700 with 16 days to first payment and 4 repayments`` () =
            let actual = monthlyParameters 700_00L<Cent> 16<DurationDay> 4 |> calculate AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/PaymentScheduleTest.Monthly028.md")
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 107<OffsetDay>
                LevelPayment = 266_12L<Cent>
                FinalPayment = 266_10L<Cent>
                PaymentTotal = 1064_46L<Cent>
                PrincipalTotal = 700_00L<Cent>
                InterestTotal = 364_46L<Cent>
                Apr = fst actual.Value.Apr, ValueSome(Percent 1309.1m)
                CostToBorrowingRatio = Percent 52.07m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£0700 with 20 days to first payment and 4 repayments`` () =
            let actual = monthlyParameters 700_00L<Cent> 20<DurationDay> 4 |> calculate AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/PaymentScheduleTest.Monthly036.md")
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 111<OffsetDay>
                LevelPayment = 273_65L<Cent>
                FinalPayment = 273_65L<Cent>
                PaymentTotal = 1094_60L<Cent>
                PrincipalTotal = 700_00L<Cent>
                InterestTotal = 394_60L<Cent>
                Apr = fst actual.Value.Apr, ValueSome(Percent 1299.5m)
                CostToBorrowingRatio = Percent 56.37m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£0700 with 24 days to first payment and 4 repayments`` () =
            let actual = monthlyParameters 700_00L<Cent> 24<DurationDay> 4 |> calculate AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/PaymentScheduleTest.Monthly044.md")
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 115<OffsetDay>
                LevelPayment = 280_39L<Cent>
                FinalPayment = 280_41L<Cent>
                PaymentTotal = 1121_58L<Cent>
                PrincipalTotal = 700_00L<Cent>
                InterestTotal = 421_58L<Cent>
                Apr = fst actual.Value.Apr, ValueSome(Percent 1287.7m)
                CostToBorrowingRatio = Percent 60.23m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£0700 with 28 days to first payment and 4 repayments`` () =
            let actual = monthlyParameters 700_00L<Cent> 28<DurationDay> 4 |> calculate AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/PaymentScheduleTest.Monthly052.md")
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 119<OffsetDay>
                LevelPayment = 287_91L<Cent>
                FinalPayment = 287_90L<Cent>
                PaymentTotal = 1151_63L<Cent>
                PrincipalTotal = 700_00L<Cent>
                InterestTotal = 451_63L<Cent>
                Apr = fst actual.Value.Apr, ValueSome(Percent 1269.4m)
                CostToBorrowingRatio = Percent 64.52m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£0700 with 32 days to first payment and 4 repayments`` () =
            let actual = monthlyParameters 700_00L<Cent> 32<DurationDay> 4 |> calculate AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/PaymentScheduleTest.Monthly060.md")
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 123<OffsetDay>
                LevelPayment = 295_42L<Cent>
                FinalPayment = 295_39L<Cent>
                PaymentTotal = 1181_65L<Cent>
                PrincipalTotal = 700_00L<Cent>
                InterestTotal = 481_65L<Cent>
                Apr = fst actual.Value.Apr, ValueSome(Percent 1248.4m)
                CostToBorrowingRatio = Percent 68.81m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£0900 with 04 days to first payment and 6 repayments`` () =
            let actual = monthlyParameters 900_00L<Cent> 4<DurationDay> 6 |> calculate AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/PaymentScheduleTest.Monthly005.md")
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 156<OffsetDay>
                LevelPayment = 249_51L<Cent>
                FinalPayment = 249_48L<Cent>
                PaymentTotal = 1497_03L<Cent>
                PrincipalTotal = 900_00L<Cent>
                InterestTotal = 597_03L<Cent>
                Apr = fst actual.Value.Apr, ValueSome(Percent 1277.8m)
                CostToBorrowingRatio = Percent 66.34m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£0900 with 08 days to first payment and 6 repayments`` () =
            let actual = monthlyParameters 900_00L<Cent> 8<DurationDay> 6 |> calculate AboveZero //AroundZero finds negative principal balance first
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/PaymentScheduleTest.Monthly013.md")
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 160<OffsetDay>
                LevelPayment = 257_22L<Cent>
                FinalPayment = 257_29L<Cent>
                PaymentTotal = 1543_39L<Cent>
                PrincipalTotal = 900_00L<Cent>
                InterestTotal = 643_39L<Cent>
                Apr = fst actual.Value.Apr, ValueSome(Percent 1291.1m)
                CostToBorrowingRatio = Percent 71.49m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£0900 with 12 days to first payment and 6 repayments`` () =
            let actual = monthlyParameters 900_00L<Cent> 12<DurationDay> 6 |> calculate AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/PaymentScheduleTest.Monthly021.md")
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 164<OffsetDay>
                LevelPayment = 264_94L<Cent>
                FinalPayment = 264_95L<Cent>
                PaymentTotal = 1589_65L<Cent>
                PrincipalTotal = 900_00L<Cent>
                InterestTotal = 689_65L<Cent>
                Apr = fst actual.Value.Apr, ValueSome(Percent 1296.2m)
                CostToBorrowingRatio = Percent 76.63m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£0900 with 16 days to first payment and 6 repayments`` () =
            let actual = monthlyParameters 900_00L<Cent> 16<DurationDay> 6 |> calculate AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/PaymentScheduleTest.Monthly029.md")
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 168<OffsetDay>
                LevelPayment = 272_66L<Cent>
                FinalPayment = 272_64L<Cent>
                PaymentTotal = 1635_94L<Cent>
                PrincipalTotal = 900_00L<Cent>
                InterestTotal = 735_94L<Cent>
                Apr = fst actual.Value.Apr, ValueSome(Percent 1294.9m)
                CostToBorrowingRatio = Percent 81.77m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£0900 with 20 days to first payment and 5 repayments`` () =
            let actual = monthlyParameters 900_00L<Cent> 20<DurationDay> 5 |> calculate AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/PaymentScheduleTest.Monthly037.md")
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 142<OffsetDay>
                LevelPayment = 308_34L<Cent>
                FinalPayment = 308_35L<Cent>
                PaymentTotal = 1541_71L<Cent>
                PrincipalTotal = 900_00L<Cent>
                InterestTotal = 641_71L<Cent>
                Apr = fst actual.Value.Apr, ValueSome(Percent 1292.9m)
                CostToBorrowingRatio = Percent 71.3m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£0900 with 24 days to first payment and 5 repayments`` () =
            let actual = monthlyParameters 900_00L<Cent> 24<DurationDay> 5 |> calculate AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/PaymentScheduleTest.Monthly045.md")
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 145<OffsetDay>
                LevelPayment = 315_80L<Cent>
                FinalPayment = 315_81L<Cent>
                PaymentTotal = 1579_01L<Cent>
                PrincipalTotal = 900_00L<Cent>
                InterestTotal = 679_01L<Cent>
                Apr = fst actual.Value.Apr, ValueSome(Percent 1283.5m)
                CostToBorrowingRatio = Percent 75.45m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£0900 with 28 days to first payment and 5 repayments`` () =
            let actual = monthlyParameters 900_00L<Cent> 28<DurationDay> 5 |> calculate AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/PaymentScheduleTest.Monthly053.md")
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 149<OffsetDay>
                LevelPayment = 324_26L<Cent>
                FinalPayment = 324_26L<Cent>
                PaymentTotal = 1621_30L<Cent>
                PrincipalTotal = 900_00L<Cent>
                InterestTotal = 721_30L<Cent>
                Apr = fst actual.Value.Apr, ValueSome(Percent 1267.9m)
                CostToBorrowingRatio = Percent 80.14m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£0900 with 32 days to first payment and 5 repayments`` () =
            let actual = monthlyParameters 900_00L<Cent> 32<DurationDay> 5 |> calculate AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/PaymentScheduleTest.Monthly061.md")
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 153<OffsetDay>
                LevelPayment = 332_72L<Cent>
                FinalPayment = 332_72L<Cent>
                PaymentTotal = 1663_60L<Cent>
                PrincipalTotal = 900_00L<Cent>
                InterestTotal = 763_60L<Cent>
                Apr = fst actual.Value.Apr, ValueSome(Percent 1249.8m)
                CostToBorrowingRatio = Percent 84.84m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£1100 with 04 days to first payment and 6 repayments`` () =
            let actual = monthlyParameters 1100_00L<Cent> 4<DurationDay> 6 |> calculate AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/PaymentScheduleTest.Monthly006.md")
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 156<OffsetDay>
                LevelPayment = 304_95L<Cent>
                FinalPayment = 304_94L<Cent>
                PaymentTotal = 1829_69L<Cent>
                PrincipalTotal = 1100_00L<Cent>
                InterestTotal = 729_69L<Cent>
                Apr = fst actual.Value.Apr, ValueSome(Percent 1277.7m)
                CostToBorrowingRatio = Percent 66.34m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£1100 with 08 days to first payment and 6 repayments`` () =
            let actual = monthlyParameters 1100_00L<Cent> 8<DurationDay> 6 |> calculate AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/PaymentScheduleTest.Monthly014.md")
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 160<OffsetDay>
                LevelPayment = 314_38L<Cent>
                FinalPayment = 314_42L<Cent>
                PaymentTotal = 1886_32L<Cent>
                PrincipalTotal = 1100_00L<Cent>
                InterestTotal = 786_32L<Cent>
                Apr = fst actual.Value.Apr, ValueSome(Percent 1291m)
                CostToBorrowingRatio = Percent 71.48m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£1100 with 12 days to first payment and 6 repayments`` () =
            let actual = monthlyParameters 1100_00L<Cent> 12<DurationDay> 6 |> calculate AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/PaymentScheduleTest.Monthly022.md")
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 164<OffsetDay>
                LevelPayment = 323_82L<Cent>
                FinalPayment = 323_79L<Cent>
                PaymentTotal = 1942_89L<Cent>
                PrincipalTotal = 1100_00L<Cent>
                InterestTotal = 842_89L<Cent>
                Apr = fst actual.Value.Apr, ValueSome(Percent 1296.2m)
                CostToBorrowingRatio = Percent 76.63m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£1100 with 16 days to first payment and 6 repayments`` () =
            let actual = monthlyParameters 1100_00L<Cent> 16<DurationDay> 6 |> calculate AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/PaymentScheduleTest.Monthly030.md")
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 168<OffsetDay>
                LevelPayment = 333_25L<Cent>
                FinalPayment = 333_24L<Cent>
                PaymentTotal = 1999_49L<Cent>
                PrincipalTotal = 1100_00L<Cent>
                InterestTotal = 899_49L<Cent>
                Apr = fst actual.Value.Apr, ValueSome(Percent 1294.9m)
                CostToBorrowingRatio = Percent 81.77m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£1100 with 20 days to first payment and 5 repayments`` () =
            let actual = monthlyParameters 1100_00L<Cent> 20<DurationDay> 5 |> calculate AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/PaymentScheduleTest.Monthly038.md")
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 142<OffsetDay>
                LevelPayment = 376_86L<Cent>
                FinalPayment = 376_87L<Cent>
                PaymentTotal = 1884_31L<Cent>
                PrincipalTotal = 1100_00L<Cent>
                InterestTotal = 784_31L<Cent>
                Apr = fst actual.Value.Apr, ValueSome(Percent 1292.9m)
                CostToBorrowingRatio = Percent 71.3m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£1100 with 24 days to first payment and 5 repayments`` () =
            let actual = monthlyParameters 1100_00L<Cent> 24<DurationDay> 5 |> calculate AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/PaymentScheduleTest.Monthly046.md")
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 145<OffsetDay>
                LevelPayment = 385_98L<Cent>
                FinalPayment = 385_97L<Cent>
                PaymentTotal = 1929_89L<Cent>
                PrincipalTotal = 1100_00L<Cent>
                InterestTotal = 829_89L<Cent>
                Apr = fst actual.Value.Apr, ValueSome(Percent 1283.5m)
                CostToBorrowingRatio = Percent 75.44m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£1100 with 28 days to first payment and 5 repayments`` () =
            let actual = monthlyParameters 1100_00L<Cent> 28<DurationDay> 5 |> calculate AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/PaymentScheduleTest.Monthly054.md")
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 149<OffsetDay>
                LevelPayment = 396_32L<Cent>
                FinalPayment = 396_30L<Cent>
                PaymentTotal = 1981_58L<Cent>
                PrincipalTotal = 1100_00L<Cent>
                InterestTotal = 881_58L<Cent>
                Apr = fst actual.Value.Apr, ValueSome(Percent 1267.9m)
                CostToBorrowingRatio = Percent 80.14m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£1100 with 32 days to first payment and 5 repayments`` () =
            let actual = monthlyParameters 1100_00L<Cent> 32<DurationDay> 5 |> calculate AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/PaymentScheduleTest.Monthly062.md")
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 153<OffsetDay>
                LevelPayment = 406_66L<Cent>
                FinalPayment = 406_66L<Cent>
                PaymentTotal = 2033_30L<Cent>
                PrincipalTotal = 1100_00L<Cent>
                InterestTotal = 933_30L<Cent>
                Apr = fst actual.Value.Apr, ValueSome(Percent 1249.8m)
                CostToBorrowingRatio = Percent 84.85m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£1300 with 04 days to first payment and 6 repayments`` () =
            let actual = monthlyParameters 1300_00L<Cent> 4<DurationDay> 6 |> calculate AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/PaymentScheduleTest.Monthly007.md")
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 156<OffsetDay>
                LevelPayment = 360_40L<Cent>
                FinalPayment = 360_37L<Cent>
                PaymentTotal = 2162_37L<Cent>
                PrincipalTotal = 1300_00L<Cent>
                InterestTotal = 862_37L<Cent>
                Apr = fst actual.Value.Apr, ValueSome(Percent 1277.7m)
                CostToBorrowingRatio = Percent 66.34m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£1300 with 08 days to first payment and 6 repayments`` () =
            let actual = monthlyParameters 1300_00L<Cent> 8<DurationDay> 6 |> calculate AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/PaymentScheduleTest.Monthly015.md")
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 160<OffsetDay>
                LevelPayment = 371_54L<Cent>
                FinalPayment = 371_58L<Cent>
                PaymentTotal = 2229_28L<Cent>
                PrincipalTotal = 1300_00L<Cent>
                InterestTotal = 929_28L<Cent>
                Apr = fst actual.Value.Apr, ValueSome(Percent 1290.9m)
                CostToBorrowingRatio = Percent 71.48m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£1300 with 12 days to first payment and 6 repayments`` () =
            let actual = monthlyParameters 1300_00L<Cent> 12<DurationDay> 6 |> calculate AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/PaymentScheduleTest.Monthly023.md")
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 164<OffsetDay>
                LevelPayment = 382_69L<Cent>
                FinalPayment = 382_74L<Cent>
                PaymentTotal = 2296_19L<Cent>
                PrincipalTotal = 1300_00L<Cent>
                InterestTotal = 996_19L<Cent>
                Apr = fst actual.Value.Apr, ValueSome(Percent 1296.2m)
                CostToBorrowingRatio = Percent 76.63m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£1300 with 16 days to first payment and 6 repayments`` () =
            let actual = monthlyParameters 1300_00L<Cent> 16<DurationDay> 6 |> calculate AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/PaymentScheduleTest.Monthly031.md")
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 168<OffsetDay>
                LevelPayment = 393_84L<Cent>
                FinalPayment = 393_86L<Cent>
                PaymentTotal = 2363_06L<Cent>
                PrincipalTotal = 1300_00L<Cent>
                InterestTotal = 1063_06L<Cent>
                Apr = fst actual.Value.Apr, ValueSome(Percent 1295m)
                CostToBorrowingRatio = Percent 81.77m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£1300 with 20 days to first payment and 6 repayments`` () =
            let actual = monthlyParameters 1300_00L<Cent> 20<DurationDay> 6 |> calculate AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/PaymentScheduleTest.Monthly039.md")
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 172<OffsetDay>
                LevelPayment = 404_99L<Cent>
                FinalPayment = 404_97L<Cent>
                PaymentTotal = 2429_92L<Cent>
                PrincipalTotal = 1300_00L<Cent>
                InterestTotal = 1129_92L<Cent>
                Apr = fst actual.Value.Apr, ValueSome(Percent 1288.6m)
                CostToBorrowingRatio = Percent 86.92m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£1300 with 24 days to first payment and 6 repayments`` () =
            let actual = monthlyParameters 1300_00L<Cent> 24<DurationDay> 6 |> calculate AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/PaymentScheduleTest.Monthly047.md")
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 176<OffsetDay>
                LevelPayment = 414_91L<Cent>
                FinalPayment = 414_90L<Cent>
                PaymentTotal = 2489_45L<Cent>
                PrincipalTotal = 1300_00L<Cent>
                InterestTotal = 1189_45L<Cent>
                Apr = fst actual.Value.Apr, ValueSome(Percent 1280.4m)
                CostToBorrowingRatio = Percent 91.5m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£1300 with 28 days to first payment and 6 repayments`` () =
            let actual = monthlyParameters 1300_00L<Cent> 28<DurationDay> 6 |> calculate AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/PaymentScheduleTest.Monthly055.md")
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 180<OffsetDay>
                LevelPayment = 426_02L<Cent>
                FinalPayment = 426_04L<Cent>
                PaymentTotal = 2556_14L<Cent>
                PrincipalTotal = 1300_00L<Cent>
                InterestTotal = 1256_14L<Cent>
                Apr = fst actual.Value.Apr, ValueSome(Percent 1266.6m)
                CostToBorrowingRatio = Percent 96.63m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£1300 with 32 days to first payment and 5 repayments`` () =
            let actual = monthlyParameters 1300_00L<Cent> 32<DurationDay> 5 |> calculate AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/PaymentScheduleTest.Monthly063.md")
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 153<OffsetDay>
                LevelPayment = 480_60L<Cent>
                FinalPayment = 480_58L<Cent>
                PaymentTotal = 2402_98L<Cent>
                PrincipalTotal = 1300_00L<Cent>
                InterestTotal = 1102_98L<Cent>
                Apr = fst actual.Value.Apr, ValueSome(Percent 1249.8m)
                CostToBorrowingRatio = Percent 84.84m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£1500 with 04 days to first payment and 6 repayments`` () =
            let actual = monthlyParameters 1500_00L<Cent> 4<DurationDay> 6 |> calculate AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/PaymentScheduleTest.Monthly008.md")
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 156<OffsetDay>
                LevelPayment = 415_84L<Cent>
                FinalPayment = 415_86L<Cent>
                PaymentTotal = 2495_06L<Cent>
                PrincipalTotal = 1500_00L<Cent>
                InterestTotal = 995_06L<Cent>
                Apr = fst actual.Value.Apr, ValueSome(Percent 1277.7m)
                CostToBorrowingRatio = Percent 66.34m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£1500 with 08 days to first payment and 6 repayments`` () =
            let actual = monthlyParameters 1500_00L<Cent> 8<DurationDay> 6 |> calculate AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/PaymentScheduleTest.Monthly016.md")
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 160<OffsetDay>
                LevelPayment = 428_71L<Cent>
                FinalPayment = 428_65L<Cent>
                PaymentTotal = 2572_20L<Cent>
                PrincipalTotal = 1500_00L<Cent>
                InterestTotal = 1072_20L<Cent>
                Apr = fst actual.Value.Apr, ValueSome(Percent 1290.9m)
                CostToBorrowingRatio = Percent 71.48m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£1500 with 12 days to first payment and 6 repayments`` () =
            let actual = monthlyParameters 1500_00L<Cent> 12<DurationDay> 6 |> calculate AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/PaymentScheduleTest.Monthly024.md")
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 164<OffsetDay>
                LevelPayment = 441_57L<Cent>
                FinalPayment = 441_57L<Cent>
                PaymentTotal = 2649_42L<Cent>
                PrincipalTotal = 1500_00L<Cent>
                InterestTotal = 1149_42L<Cent>
                Apr = fst actual.Value.Apr, ValueSome(Percent 1296.2m)
                CostToBorrowingRatio = Percent 76.63m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£1500 with 16 days to first payment and 6 repayments`` () =
            let actual = monthlyParameters 1500_00L<Cent> 16<DurationDay> 6 |> calculate AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/PaymentScheduleTest.Monthly032.md")
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 168<OffsetDay>
                LevelPayment = 454_43L<Cent>
                FinalPayment = 454_45L<Cent>
                PaymentTotal = 2726_60L<Cent>
                PrincipalTotal = 1500_00L<Cent>
                InterestTotal = 1226_60L<Cent>
                Apr = fst actual.Value.Apr, ValueSome(Percent 1295m)
                CostToBorrowingRatio = Percent 81.77m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£1500 with 20 days to first payment and 6 repayments`` () =
            let actual = monthlyParameters 1500_00L<Cent> 20<DurationDay> 6 |> calculate AboveZero //AroundZero finds negative principal balance first
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/PaymentScheduleTest.Monthly040.md")
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 172<OffsetDay>
                LevelPayment = 467_29L<Cent>
                FinalPayment = 467_33L<Cent>
                PaymentTotal = 2803_78L<Cent>
                PrincipalTotal = 1500_00L<Cent>
                InterestTotal = 1303_78L<Cent>
                Apr = fst actual.Value.Apr, ValueSome(Percent 1288.6m)
                CostToBorrowingRatio = Percent 86.92m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£1500 with 24 days to first payment and 6 repayments`` () =
            let actual = monthlyParameters 1500_00L<Cent> 24<DurationDay> 6 |> calculate AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/PaymentScheduleTest.Monthly048.md")
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 176<OffsetDay>
                LevelPayment = 478_74L<Cent>
                FinalPayment = 478_76L<Cent>
                PaymentTotal = 2872_46L<Cent>
                PrincipalTotal = 1500_00L<Cent>
                InterestTotal = 1372_46L<Cent>
                Apr = fst actual.Value.Apr, ValueSome(Percent 1280.4m)
                CostToBorrowingRatio = Percent 91.5m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£1500 with 28 days to first payment and 6 repayments`` () =
            let actual = monthlyParameters 1500_00L<Cent> 28<DurationDay> 6 |> calculate AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/PaymentScheduleTest.Monthly056.md")
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 180<OffsetDay>
                LevelPayment = 491_57L<Cent>
                FinalPayment = 491_52L<Cent>
                PaymentTotal = 2949_37L<Cent>
                PrincipalTotal = 1500_00L<Cent>
                InterestTotal = 1449_37L<Cent>
                Apr = fst actual.Value.Apr, ValueSome(Percent 1266.6m)
                CostToBorrowingRatio = Percent 96.62m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£1500 with 32 days to first payment and 5 repayments`` () =
            let actual = monthlyParameters 1500_00L<Cent> 32<DurationDay> 5 |> calculate AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/PaymentScheduleTest.Monthly064.md")
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 153<OffsetDay>
                LevelPayment = 554_53L<Cent>
                FinalPayment = 554_57L<Cent>
                PaymentTotal = 2772_69L<Cent>
                PrincipalTotal = 1500_00L<Cent>
                InterestTotal = 1272_69L<Cent>
                Apr = fst actual.Value.Apr, ValueSome(Percent 1249.8m)
                CostToBorrowingRatio = Percent 84.85m
            }
            actual |> should equal expected


    [<Fact>]
    let ``1) If there are no other payments, level payment should equal final payment`` () =
        let sp = {
            AsOfDate = Date(2022, 12, 19)
            ScheduleType = ScheduleType.Original
            StartDate = Date(2022, 12, 19)
            Principal = 300_00L<Cent>
            PaymentSchedule = RegularSchedule (
                UnitPeriodConfig = UnitPeriod.Daily(Date(2023, 1, 3)),
                PaymentCount = 1
            )
            FeesAndCharges = {
                Fees = [||]
                FeesAmortisation = Fees.FeeAmortisation.AmortiseProportionately
                FeesSettlementRefund = Fees.SettlementRefund.ProRata
                Charges = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 3<DurationDay>
            }
            Interest = {
                StandardRate = Interest.Rate.Daily (Percent 0.8m)
                Cap = Interest.Cap.example
                InitialGracePeriod = 0<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = ValueSome <| Interest.Rate.Annual (Percent 8m)
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                RoundingOptions = RoundingOptions.recommended
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
            }
        }

        let actual =
            voption {
                let! schedule = sp |> calculate AroundZero
                schedule.Items |> Formatting.outputListToHtml "out/PaymentSchedule001.md"
                return schedule.LevelPayment, schedule.FinalPayment
            }

        let expected = ValueSome (336_00L<Cent>, 336_00L<Cent>)
        actual |> should equal expected
