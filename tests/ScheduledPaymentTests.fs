namespace FSharp.Finance.Personal.Tests

open System
open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal

module ScheduledPaymentTests =

    open ScheduledPayment

    module Biweekly =
        let biweeklyParameters principal offset =
            let startDate = Date(2023, 11, 15)
            {
                AsOfDate = startDate
                StartDate = startDate
                Principal = principal
                UnitPeriodConfig = UnitPeriod.Weekly(2, startDate.AddDays(int offset))
                PaymentCount = 11
                FeesAndCharges = {
                    Fees = ValueSome <| Fees.Percentage (Percent 189.47m, ValueNone)
                    FeesSettlement = Fees.Settlement.ProRataRefund
                    Charges = [| Charge.InsufficientFunds 750L<Cent>; Charge.LatePayment 1000L<Cent> |]
                    LatePaymentGracePeriod = 0<DurationDay>
                }
                Interest = {
                    Rate = Interest.Rate.Annual (Percent 9.95m)
                    Cap = { Total = ValueNone; Daily = ValueNone }
                    GracePeriod = 3<DurationDay>
                    Holidays = [||]
                }
                Calculation = {
                    AprMethod = Apr.CalculationMethod.UsActuarial 8
                    RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                    FinalPaymentAdjustment = AdjustFinalPayment
                }
            }

        [<Fact>]
        let ``$1200 with short first period`` () =
            let actual = biweeklyParameters 120000L<Cent> 8<DurationDay> |> calculateSchedule BelowZero
            actual |> ValueOption.iter(_.Items >> Formatting.outputListToHtml "out/ScheduledPaymentTest.Biweekly001.md" (ValueSome 300))
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 148<OffsetDay>
                LevelPayment = 32253L<Cent>
                FinalPayment = 32253L<Cent>
                PaymentTotal = 354783L<Cent>
                PrincipalTotal = 347364L<Cent>
                InterestTotal = 7419L<Cent>
                Apr = Percent 717.412507m
                CostToBorrowingRatio = Percent 67.59m
            }
            actual |> should equal expected

        [<Fact>]
        let ``2) $1200 with first period equal to unit-period length`` () =
            let actual = biweeklyParameters 120000L<Cent> 14<DurationDay> |> calculateSchedule BelowZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/ScheduledPaymentTest.Biweekly002.md" (ValueSome 300))
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 154<OffsetDay>
                LevelPayment = 32306L<Cent>
                FinalPayment = 32303L<Cent>
                PaymentTotal = 355363L<Cent>
                PrincipalTotal = 347364L<Cent>
                InterestTotal = 7999L<Cent>
                Apr = Percent 637.159359m
                CostToBorrowingRatio = Percent 67.76m
            }
            actual |> should equal expected

        [<Fact>]
        let ``3) $1200 with long first period`` () =
            let actual = biweeklyParameters 120000L<Cent> 15<DurationDay> |> calculateSchedule BelowZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/ScheduledPaymentTest.Biweekly003.md" (ValueSome 300))
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 155<OffsetDay>
                LevelPayment = 32315L<Cent>
                FinalPayment = 32310L<Cent>
                PaymentTotal = 355460L<Cent>
                PrincipalTotal = 347364L<Cent>
                InterestTotal = 8096L<Cent>
                Apr = Percent 623.703586m
                CostToBorrowingRatio = Percent 67.78m
            }
            actual |> should equal expected

    module Monthly =

        let monthlyParameters principal offset paymentCount =
            let startDate = Date(2023, 12, 07)
            {
                AsOfDate = startDate
                StartDate = startDate
                Principal = principal
                UnitPeriodConfig = startDate.AddDays(int offset) |> fun d -> UnitPeriod.Monthly(1, d.Year, d.Month, d.Day * 1)
                PaymentCount = paymentCount
                FeesAndCharges = {
                    Fees = ValueNone
                    FeesSettlement = Fees.Settlement.ProRataRefund
                    Charges = [||]
                    LatePaymentGracePeriod = 0<DurationDay>
                }
                Interest = {
                    Rate = Interest.Rate.Daily (Percent 0.798m)
                    Cap = { Total = ValueSome (Interest.TotalPercentageCap (Percent 100m)); Daily = ValueSome (Interest.DailyPercentageCap (Percent 0.8m)) }
                    GracePeriod = 3<DurationDay>
                    Holidays = [||]
                }
                Calculation = {
                    AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                    RoundingOptions = { InterestRounding = Round MidpointRounding.AwayFromZero; PaymentRounding = Round MidpointRounding.AwayFromZero }
                    FinalPaymentAdjustment = AdjustFinalPayment
                }
            }

        [<Fact>]
        let ``£0100 with 04 days to first payment and 5 repayments`` () =
            let actual = monthlyParameters 10000L<Cent> 4<DurationDay> 5 |> calculateSchedule AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/ScheduledPaymentTest.Monthly001.md" (ValueSome 300))
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 126<OffsetDay>
                LevelPayment = 3049L<Cent>
                FinalPayment = 3046L<Cent>
                PaymentTotal = 15242L<Cent>
                PrincipalTotal = 10000L<Cent>
                InterestTotal = 5242L<Cent>
                Apr = Percent 1280.8m
                CostToBorrowingRatio = Percent 52.42m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£0100 with 08 days to first payment and 5 repayments`` () =
            let actual = monthlyParameters 10000L<Cent> 8<DurationDay> 5 |> calculateSchedule AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/ScheduledPaymentTest.Monthly009.md" (ValueSome 300))
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 130<OffsetDay>
                LevelPayment = 3143L<Cent>
                FinalPayment = 3142L<Cent>
                PaymentTotal = 15714L<Cent>
                PrincipalTotal = 10000L<Cent>
                InterestTotal = 5714L<Cent>
                Apr = Percent 1295.9m
                CostToBorrowingRatio = Percent 57.14m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£0100 with 12 days to first payment and 4 repayments`` () =
            let actual = monthlyParameters 10000L<Cent> 12<DurationDay> 4 |> calculateSchedule AboveZero //AroundZero finds negative principal balance first
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/ScheduledPaymentTest.Monthly017.md" (ValueSome 300))
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 103<OffsetDay>
                LevelPayment = 3694L<Cent>
                FinalPayment = 3695L<Cent>
                PaymentTotal = 14777L<Cent>
                PrincipalTotal = 10000L<Cent>
                InterestTotal = 4777L<Cent>
                Apr = Percent 1312.3m
                CostToBorrowingRatio = Percent 47.77m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£0100 with 16 days to first payment and 4 repayments`` () =
            let actual = monthlyParameters 10000L<Cent> 16<DurationDay> 4 |> calculateSchedule BelowZero //AroundZero finds positive principal balance first
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/ScheduledPaymentTest.Monthly025.md" (ValueSome 300))
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 107<OffsetDay>
                LevelPayment = 3802L<Cent>
                FinalPayment = 3800L<Cent>
                PaymentTotal = 15206L<Cent>
                PrincipalTotal = 10000L<Cent>
                InterestTotal = 5206L<Cent>
                Apr = Percent 1309m
                CostToBorrowingRatio = Percent 52.06m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£0100 with 20 days to first payment and 4 repayments`` () =
            let actual = monthlyParameters 10000L<Cent> 20<DurationDay> 4 |> calculateSchedule AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/ScheduledPaymentTest.Monthly033.md" (ValueSome 300))
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 111<OffsetDay>
                LevelPayment = 3909L<Cent>
                FinalPayment = 3911L<Cent>
                PaymentTotal = 15638L<Cent>
                PrincipalTotal = 10000L<Cent>
                InterestTotal = 5638L<Cent>
                Apr = Percent 1299.7m
                CostToBorrowingRatio = Percent 56.38m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£0100 with 24 days to first payment and 4 repayments`` () =
            let actual = monthlyParameters 10000L<Cent> 24<DurationDay> 4 |> calculateSchedule BelowZero //AroundZero finds positive principal balance first
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/ScheduledPaymentTest.Monthly041.md" (ValueSome 300))
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 115<OffsetDay>
                LevelPayment = 4006L<Cent>
                FinalPayment = 4004L<Cent>
                PaymentTotal = 16022L<Cent>
                PrincipalTotal = 10000L<Cent>
                InterestTotal = 6022L<Cent>
                Apr = Percent 1287.7m
                CostToBorrowingRatio = Percent 60.22m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£0100 with 28 days to first payment and 4 repayments`` () =
            let actual = monthlyParameters 10000L<Cent> 28<DurationDay> 4 |> calculateSchedule BelowZero //AroundZero finds positive principal balance first
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/ScheduledPaymentTest.Monthly049.md" (ValueSome 300))
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 119<OffsetDay>
                LevelPayment = 4113L<Cent>
                FinalPayment = 4111L<Cent>
                PaymentTotal = 16450L<Cent>
                PrincipalTotal = 10000L<Cent>
                InterestTotal = 6450L<Cent>
                Apr = Percent 1268.8m
                CostToBorrowingRatio = Percent 64.5m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£0100 with 32 days to first payment and 4 repayments`` () =
            let actual = monthlyParameters 10000L<Cent> 32<DurationDay> 4 |> calculateSchedule AboveZero //AroundZero finds negative principal balance first
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/ScheduledPaymentTest.Monthly057.md" (ValueSome 300))
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 123<OffsetDay>
                LevelPayment = 4220L<Cent>
                FinalPayment = 4222L<Cent>
                PaymentTotal = 16882L<Cent>
                PrincipalTotal = 10000L<Cent>
                InterestTotal = 6882L<Cent>
                Apr = Percent 1248.6m
                CostToBorrowingRatio = Percent 68.82m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£0300 with 04 days to first payment and 5 repayments`` () =
            let actual = monthlyParameters 30000L<Cent> 4<DurationDay> 5 |> calculateSchedule AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/ScheduledPaymentTest.Monthly002.md" (ValueSome 300))
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 126<OffsetDay>
                LevelPayment = 9146L<Cent>
                FinalPayment = 9150L<Cent>
                PaymentTotal = 45734L<Cent>
                PrincipalTotal = 30000L<Cent>
                InterestTotal = 15734L<Cent>
                Apr = Percent 1281.4m
                CostToBorrowingRatio = Percent 52.45m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£0300 with 08 days to first payment and 5 repayments`` () =
            let actual = monthlyParameters 30000L<Cent> 8<DurationDay> 5 |> calculateSchedule AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/ScheduledPaymentTest.Monthly010.md" (ValueSome 300))
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 130<OffsetDay>
                LevelPayment = 9429L<Cent>
                FinalPayment = 9431L<Cent>
                PaymentTotal = 47147L<Cent>
                PrincipalTotal = 30000L<Cent>
                InterestTotal = 17147L<Cent>
                Apr = Percent 1296.5m
                CostToBorrowingRatio = Percent 57.16m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£0300 with 12 days to first payment and 4 repayments`` () =
            let actual = monthlyParameters 30000L<Cent> 12<DurationDay> 4 |> calculateSchedule AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/ScheduledPaymentTest.Monthly018.md" (ValueSome 300))
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 103<OffsetDay>
                LevelPayment = 11082L<Cent>
                FinalPayment = 11084L<Cent>
                PaymentTotal = 44330L<Cent>
                PrincipalTotal = 30000L<Cent>
                InterestTotal = 14330L<Cent>
                Apr = Percent 1312.1m
                CostToBorrowingRatio = Percent 47.77m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£0300 with 16 days to first payment and 4 repayments`` () =
            let actual = monthlyParameters 30000L<Cent> 16<DurationDay> 4 |> calculateSchedule AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/ScheduledPaymentTest.Monthly026.md" (ValueSome 300))
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 107<OffsetDay>
                LevelPayment = 11405L<Cent>
                FinalPayment = 11403L<Cent>
                PaymentTotal = 45618L<Cent>
                PrincipalTotal = 30000L<Cent>
                InterestTotal = 15618L<Cent>
                Apr = Percent 1308.8m
                CostToBorrowingRatio = Percent 52.06m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£0300 with 20 days to first payment and 4 repayments`` () =
            let actual = monthlyParameters 30000L<Cent> 20<DurationDay> 4 |> calculateSchedule AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/ScheduledPaymentTest.Monthly034.md" (ValueSome 300))
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 111<OffsetDay>
                LevelPayment = 11728L<Cent>
                FinalPayment = 11728L<Cent>
                PaymentTotal = 46912L<Cent>
                PrincipalTotal = 30000L<Cent>
                InterestTotal = 16912L<Cent>
                Apr = Percent 1299.6m
                CostToBorrowingRatio = Percent 56.37m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£0300 with 24 days to first payment and 4 repayments`` () =
            let actual = monthlyParameters 30000L<Cent> 24<DurationDay> 4 |> calculateSchedule AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/ScheduledPaymentTest.Monthly042.md" (ValueSome 300))
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 115<OffsetDay>
                LevelPayment = 12017L<Cent>
                FinalPayment = 12017L<Cent>
                PaymentTotal = 48068L<Cent>
                PrincipalTotal = 30000L<Cent>
                InterestTotal = 18068L<Cent>
                Apr = Percent 1287.8m
                CostToBorrowingRatio = Percent 60.23m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£0300 with 28 days to first payment and 4 repayments`` () =
            let actual = monthlyParameters 30000L<Cent> 28<DurationDay> 4 |> calculateSchedule BelowZero //AroundZero finds positive principal balance first
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/ScheduledPaymentTest.Monthly050.md" (ValueSome 300))
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 119<OffsetDay>
                LevelPayment = 12339L<Cent>
                FinalPayment = 12338L<Cent>
                PaymentTotal = 49355L<Cent>
                PrincipalTotal = 30000L<Cent>
                InterestTotal = 19355L<Cent>
                Apr = Percent 1269.3m
                CostToBorrowingRatio = Percent 64.52m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£0300 with 32 days to first payment and 4 repayments`` () =
            let actual = monthlyParameters 30000L<Cent> 32<DurationDay> 4 |> calculateSchedule AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/ScheduledPaymentTest.Monthly058.md" (ValueSome 300))
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 123<OffsetDay>
                LevelPayment = 12661L<Cent>
                FinalPayment = 12661L<Cent>
                PaymentTotal = 50644L<Cent>
                PrincipalTotal = 30000L<Cent>
                InterestTotal = 20644L<Cent>
                Apr = Percent 1248.6m
                CostToBorrowingRatio = Percent 68.81m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£0500 with 04 days to first payment and 5 repayments`` () =
            let actual = monthlyParameters 50000L<Cent> 4<DurationDay> 5 |> calculateSchedule AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/ScheduledPaymentTest.Monthly003.md" (ValueSome 300))
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 126<OffsetDay>
                LevelPayment = 15244L<Cent>
                FinalPayment = 15243L<Cent>
                PaymentTotal = 76219L<Cent>
                PrincipalTotal = 50000L<Cent>
                InterestTotal = 26219L<Cent>
                Apr = Percent 1281.2m
                CostToBorrowingRatio = Percent 52.44m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£0500 with 08 days to first payment and 5 repayments`` () =
            let actual = monthlyParameters 50000L<Cent> 8<DurationDay> 5 |> calculateSchedule AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/ScheduledPaymentTest.Monthly011.md" (ValueSome 300))
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 130<OffsetDay>
                LevelPayment = 15715L<Cent>
                FinalPayment = 15719L<Cent>
                PaymentTotal = 78579L<Cent>
                PrincipalTotal = 50000L<Cent>
                InterestTotal = 28579L<Cent>
                Apr = Percent 1296.6m
                CostToBorrowingRatio = Percent 57.16m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£0500 with 12 days to first payment and 4 repayments`` () =
            let actual = monthlyParameters 50000L<Cent> 12<DurationDay> 4 |> calculateSchedule AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/ScheduledPaymentTest.Monthly019.md" (ValueSome 300))
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 103<OffsetDay>
                LevelPayment = 18470L<Cent>
                FinalPayment = 18471L<Cent>
                PaymentTotal = 73881L<Cent>
                PrincipalTotal = 50000L<Cent>
                InterestTotal = 23881L<Cent>
                Apr = Percent 1311.9m
                CostToBorrowingRatio = Percent 47.76m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£0500 with 16 days to first payment and 4 repayments`` () =
            let actual = monthlyParameters 50000L<Cent> 16<DurationDay> 4 |> calculateSchedule AboveZero //AroundZero finds negative principal balance first
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/ScheduledPaymentTest.Monthly027.md" (ValueSome 300))
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 107<OffsetDay>
                LevelPayment = 19008L<Cent>
                FinalPayment = 19009L<Cent>
                PaymentTotal = 76033L<Cent>
                PrincipalTotal = 50000L<Cent>
                InterestTotal = 26033L<Cent>
                Apr = Percent 1309m
                CostToBorrowingRatio = Percent 52.07m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£0500 with 20 days to first payment and 4 repayments`` () =
            let actual = monthlyParameters 50000L<Cent> 20<DurationDay> 4 |> calculateSchedule AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/ScheduledPaymentTest.Monthly035.md" (ValueSome 300))
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 111<OffsetDay>
                LevelPayment = 19546L<Cent>
                FinalPayment = 19549L<Cent>
                PaymentTotal = 78187L<Cent>
                PrincipalTotal = 50000L<Cent>
                InterestTotal = 28187L<Cent>
                Apr = Percent 1299.6m
                CostToBorrowingRatio = Percent 56.37m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£0500 with 24 days to first payment and 4 repayments`` () =
            let actual = monthlyParameters 50000L<Cent> 24<DurationDay> 4 |> calculateSchedule AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/ScheduledPaymentTest.Monthly043.md" (ValueSome 300))
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 115<OffsetDay>
                LevelPayment = 20028L<Cent>
                FinalPayment = 20028L<Cent>
                PaymentTotal = 80112L<Cent>
                PrincipalTotal = 50000L<Cent>
                InterestTotal = 30112L<Cent>
                Apr = Percent 1287.6m
                CostToBorrowingRatio = Percent 60.22m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£0500 with 28 days to first payment and 4 repayments`` () =
            let actual = monthlyParameters 50000L<Cent> 28<DurationDay> 4 |> calculateSchedule BelowZero //AroundZero finds positive principal balance first
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/ScheduledPaymentTest.Monthly051.md" (ValueSome 300))
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 119<OffsetDay>
                LevelPayment = 20565L<Cent>
                FinalPayment = 20563L<Cent>
                PaymentTotal = 82258L<Cent>
                PrincipalTotal = 50000L<Cent>
                InterestTotal = 32258L<Cent>
                Apr = Percent 1269.3m
                CostToBorrowingRatio = Percent 64.52m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£0500 with 32 days to first payment and 4 repayments`` () =
            let actual = monthlyParameters 50000L<Cent> 32<DurationDay> 4 |> calculateSchedule AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/ScheduledPaymentTest.Monthly059.md" (ValueSome 300))
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 123<OffsetDay>
                LevelPayment = 21101L<Cent>
                FinalPayment = 21103L<Cent>
                PaymentTotal = 84406L<Cent>
                PrincipalTotal = 50000L<Cent>
                InterestTotal = 34406L<Cent>
                Apr = Percent 1248.5m
                CostToBorrowingRatio = Percent 68.81m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£0700 with 04 days to first payment and 5 repayments`` () =
            let actual = monthlyParameters 70000L<Cent> 4<DurationDay> 5 |> calculateSchedule BelowZero //AroundZero finds positive principal balance first
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/ScheduledPaymentTest.Monthly004.md" (ValueSome 300))
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 126<OffsetDay>
                LevelPayment = 21342L<Cent>
                FinalPayment = 21339L<Cent>
                PaymentTotal = 106707L<Cent>
                PrincipalTotal = 70000L<Cent>
                InterestTotal = 36707L<Cent>
                Apr = Percent 1281.3m
                CostToBorrowingRatio = Percent 52.44m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£0700 with 08 days to first payment and 5 repayments`` () =
            let actual = monthlyParameters 70000L<Cent> 8<DurationDay> 5 |> calculateSchedule AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/ScheduledPaymentTest.Monthly012.md" (ValueSome 300))
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 130<OffsetDay>
                LevelPayment = 22002L<Cent>
                FinalPayment = 21999L<Cent>
                PaymentTotal = 110007L<Cent>
                PrincipalTotal = 70000L<Cent>
                InterestTotal = 40007L<Cent>
                Apr = Percent 1296.5m
                CostToBorrowingRatio = Percent 57.15m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£0700 with 12 days to first payment and 4 repayments`` () =
            let actual = monthlyParameters 70000L<Cent> 12<DurationDay> 4 |> calculateSchedule AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/ScheduledPaymentTest.Monthly020.md" (ValueSome 300))
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 103<OffsetDay>
                LevelPayment = 25858L<Cent>
                FinalPayment = 25860L<Cent>
                PaymentTotal = 103434L<Cent>
                PrincipalTotal = 70000L<Cent>
                InterestTotal = 33434L<Cent>
                Apr = Percent 1311.9m
                CostToBorrowingRatio = Percent 47.76m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£0700 with 16 days to first payment and 4 repayments`` () =
            let actual = monthlyParameters 70000L<Cent> 16<DurationDay> 4 |> calculateSchedule AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/ScheduledPaymentTest.Monthly028.md" (ValueSome 300))
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 107<OffsetDay>
                LevelPayment = 26612L<Cent>
                FinalPayment = 26610L<Cent>
                PaymentTotal = 106446L<Cent>
                PrincipalTotal = 70000L<Cent>
                InterestTotal = 36446L<Cent>
                Apr = Percent 1309.1m
                CostToBorrowingRatio = Percent 52.07m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£0700 with 20 days to first payment and 4 repayments`` () =
            let actual = monthlyParameters 70000L<Cent> 20<DurationDay> 4 |> calculateSchedule AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/ScheduledPaymentTest.Monthly036.md" (ValueSome 300))
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 111<OffsetDay>
                LevelPayment = 27365L<Cent>
                FinalPayment = 27365L<Cent>
                PaymentTotal = 109460L<Cent>
                PrincipalTotal = 70000L<Cent>
                InterestTotal = 39460L<Cent>
                Apr = Percent 1299.5m
                CostToBorrowingRatio = Percent 56.37m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£0700 with 24 days to first payment and 4 repayments`` () =
            let actual = monthlyParameters 70000L<Cent> 24<DurationDay> 4 |> calculateSchedule AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/ScheduledPaymentTest.Monthly044.md" (ValueSome 300))
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 115<OffsetDay>
                LevelPayment = 28039L<Cent>
                FinalPayment = 28041L<Cent>
                PaymentTotal = 112158L<Cent>
                PrincipalTotal = 70000L<Cent>
                InterestTotal = 42158L<Cent>
                Apr = Percent 1287.7m
                CostToBorrowingRatio = Percent 60.23m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£0700 with 28 days to first payment and 4 repayments`` () =
            let actual = monthlyParameters 70000L<Cent> 28<DurationDay> 4 |> calculateSchedule AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/ScheduledPaymentTest.Monthly052.md" (ValueSome 300))
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 119<OffsetDay>
                LevelPayment = 28791L<Cent>
                FinalPayment = 28790L<Cent>
                PaymentTotal = 115163L<Cent>
                PrincipalTotal = 70000L<Cent>
                InterestTotal = 45163L<Cent>
                Apr = Percent 1269.4m
                CostToBorrowingRatio = Percent 64.52m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£0700 with 32 days to first payment and 4 repayments`` () =
            let actual = monthlyParameters 70000L<Cent> 32<DurationDay> 4 |> calculateSchedule AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/ScheduledPaymentTest.Monthly060.md" (ValueSome 300))
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 123<OffsetDay>
                LevelPayment = 29542L<Cent>
                FinalPayment = 29539L<Cent>
                PaymentTotal = 118165L<Cent>
                PrincipalTotal = 70000L<Cent>
                InterestTotal = 48165L<Cent>
                Apr = Percent 1248.4m
                CostToBorrowingRatio = Percent 68.81m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£0900 with 04 days to first payment and 6 repayments`` () =
            let actual = monthlyParameters 90000L<Cent> 4<DurationDay> 6 |> calculateSchedule AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/ScheduledPaymentTest.Monthly005.md" (ValueSome 300))
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 156<OffsetDay>
                LevelPayment = 24951L<Cent>
                FinalPayment = 24948L<Cent>
                PaymentTotal = 149703L<Cent>
                PrincipalTotal = 90000L<Cent>
                InterestTotal = 59703L<Cent>
                Apr = Percent 1277.8m
                CostToBorrowingRatio = Percent 66.34m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£0900 with 08 days to first payment and 6 repayments`` () =
            let actual = monthlyParameters 90000L<Cent> 8<DurationDay> 6 |> calculateSchedule AboveZero //AroundZero finds negative principal balance first
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/ScheduledPaymentTest.Monthly013.md" (ValueSome 300))
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 160<OffsetDay>
                LevelPayment = 25722L<Cent>
                FinalPayment = 25729L<Cent>
                PaymentTotal = 154339L<Cent>
                PrincipalTotal = 90000L<Cent>
                InterestTotal = 64339L<Cent>
                Apr = Percent 1291.1m
                CostToBorrowingRatio = Percent 71.49m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£0900 with 12 days to first payment and 6 repayments`` () =
            let actual = monthlyParameters 90000L<Cent> 12<DurationDay> 6 |> calculateSchedule AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/ScheduledPaymentTest.Monthly021.md" (ValueSome 300))
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 164<OffsetDay>
                LevelPayment = 26494L<Cent>
                FinalPayment = 26495L<Cent>
                PaymentTotal = 158965L<Cent>
                PrincipalTotal = 90000L<Cent>
                InterestTotal = 68965L<Cent>
                Apr = Percent 1296.2m
                CostToBorrowingRatio = Percent 76.63m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£0900 with 16 days to first payment and 6 repayments`` () =
            let actual = monthlyParameters 90000L<Cent> 16<DurationDay> 6 |> calculateSchedule AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/ScheduledPaymentTest.Monthly029.md" (ValueSome 300))
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 168<OffsetDay>
                LevelPayment = 27266L<Cent>
                FinalPayment = 27264L<Cent>
                PaymentTotal = 163594L<Cent>
                PrincipalTotal = 90000L<Cent>
                InterestTotal = 73594L<Cent>
                Apr = Percent 1294.9m
                CostToBorrowingRatio = Percent 81.77m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£0900 with 20 days to first payment and 5 repayments`` () =
            let actual = monthlyParameters 90000L<Cent> 20<DurationDay> 5 |> calculateSchedule AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/ScheduledPaymentTest.Monthly037.md" (ValueSome 300))
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 142<OffsetDay>
                LevelPayment = 30834L<Cent>
                FinalPayment = 30835L<Cent>
                PaymentTotal = 154171L<Cent>
                PrincipalTotal = 90000L<Cent>
                InterestTotal = 64171L<Cent>
                Apr = Percent 1292.9m
                CostToBorrowingRatio = Percent 71.3m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£0900 with 24 days to first payment and 5 repayments`` () =
            let actual = monthlyParameters 90000L<Cent> 24<DurationDay> 5 |> calculateSchedule AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/ScheduledPaymentTest.Monthly045.md" (ValueSome 300))
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 145<OffsetDay>
                LevelPayment = 31580L<Cent>
                FinalPayment = 31581L<Cent>
                PaymentTotal = 157901L<Cent>
                PrincipalTotal = 90000L<Cent>
                InterestTotal = 67901L<Cent>
                Apr = Percent 1283.5m
                CostToBorrowingRatio = Percent 75.45m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£0900 with 28 days to first payment and 5 repayments`` () =
            let actual = monthlyParameters 90000L<Cent> 28<DurationDay> 5 |> calculateSchedule AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/ScheduledPaymentTest.Monthly053.md" (ValueSome 300))
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 149<OffsetDay>
                LevelPayment = 32426L<Cent>
                FinalPayment = 32426L<Cent>
                PaymentTotal = 162130L<Cent>
                PrincipalTotal = 90000L<Cent>
                InterestTotal = 72130L<Cent>
                Apr = Percent 1267.9m
                CostToBorrowingRatio = Percent 80.14m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£0900 with 32 days to first payment and 5 repayments`` () =
            let actual = monthlyParameters 90000L<Cent> 32<DurationDay> 5 |> calculateSchedule AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/ScheduledPaymentTest.Monthly061.md" (ValueSome 300))
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 153<OffsetDay>
                LevelPayment = 33272L<Cent>
                FinalPayment = 33272L<Cent>
                PaymentTotal = 166360L<Cent>
                PrincipalTotal = 90000L<Cent>
                InterestTotal = 76360L<Cent>
                Apr = Percent 1249.8m
                CostToBorrowingRatio = Percent 84.84m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£1100 with 04 days to first payment and 6 repayments`` () =
            let actual = monthlyParameters 110000L<Cent> 4<DurationDay> 6 |> calculateSchedule AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/ScheduledPaymentTest.Monthly006.md" (ValueSome 300))
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 156<OffsetDay>
                LevelPayment = 30495L<Cent>
                FinalPayment = 30494L<Cent>
                PaymentTotal = 182969L<Cent>
                PrincipalTotal = 110000L<Cent>
                InterestTotal = 72969L<Cent>
                Apr = Percent 1277.7m
                CostToBorrowingRatio = Percent 66.34m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£1100 with 08 days to first payment and 6 repayments`` () =
            let actual = monthlyParameters 110000L<Cent> 8<DurationDay> 6 |> calculateSchedule AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/ScheduledPaymentTest.Monthly014.md" (ValueSome 300))
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 160<OffsetDay>
                LevelPayment = 31438L<Cent>
                FinalPayment = 31442L<Cent>
                PaymentTotal = 188632L<Cent>
                PrincipalTotal = 110000L<Cent>
                InterestTotal = 78632L<Cent>
                Apr = Percent 1291m
                CostToBorrowingRatio = Percent 71.48m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£1100 with 12 days to first payment and 6 repayments`` () =
            let actual = monthlyParameters 110000L<Cent> 12<DurationDay> 6 |> calculateSchedule AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/ScheduledPaymentTest.Monthly022.md" (ValueSome 300))
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 164<OffsetDay>
                LevelPayment = 32382L<Cent>
                FinalPayment = 32379L<Cent>
                PaymentTotal = 194289L<Cent>
                PrincipalTotal = 110000L<Cent>
                InterestTotal = 84289L<Cent>
                Apr = Percent 1296.2m
                CostToBorrowingRatio = Percent 76.63m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£1100 with 16 days to first payment and 6 repayments`` () =
            let actual = monthlyParameters 110000L<Cent> 16<DurationDay> 6 |> calculateSchedule AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/ScheduledPaymentTest.Monthly030.md" (ValueSome 300))
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 168<OffsetDay>
                LevelPayment = 33325L<Cent>
                FinalPayment = 33324L<Cent>
                PaymentTotal = 199949L<Cent>
                PrincipalTotal = 110000L<Cent>
                InterestTotal = 89949L<Cent>
                Apr = Percent 1294.9m
                CostToBorrowingRatio = Percent 81.77m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£1100 with 20 days to first payment and 5 repayments`` () =
            let actual = monthlyParameters 110000L<Cent> 20<DurationDay> 5 |> calculateSchedule AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/ScheduledPaymentTest.Monthly038.md" (ValueSome 300))
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 142<OffsetDay>
                LevelPayment = 37686L<Cent>
                FinalPayment = 37687L<Cent>
                PaymentTotal = 188431L<Cent>
                PrincipalTotal = 110000L<Cent>
                InterestTotal = 78431L<Cent>
                Apr = Percent 1292.9m
                CostToBorrowingRatio = Percent 71.3m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£1100 with 24 days to first payment and 5 repayments`` () =
            let actual = monthlyParameters 110000L<Cent> 24<DurationDay> 5 |> calculateSchedule AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/ScheduledPaymentTest.Monthly046.md" (ValueSome 300))
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 145<OffsetDay>
                LevelPayment = 38598L<Cent>
                FinalPayment = 38597L<Cent>
                PaymentTotal = 192989L<Cent>
                PrincipalTotal = 110000L<Cent>
                InterestTotal = 82989L<Cent>
                Apr = Percent 1283.5m
                CostToBorrowingRatio = Percent 75.44m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£1100 with 28 days to first payment and 5 repayments`` () =
            let actual = monthlyParameters 110000L<Cent> 28<DurationDay> 5 |> calculateSchedule AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/ScheduledPaymentTest.Monthly054.md" (ValueSome 300))
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 149<OffsetDay>
                LevelPayment = 39632L<Cent>
                FinalPayment = 39630L<Cent>
                PaymentTotal = 198158L<Cent>
                PrincipalTotal = 110000L<Cent>
                InterestTotal = 88158L<Cent>
                Apr = Percent 1267.9m
                CostToBorrowingRatio = Percent 80.14m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£1100 with 32 days to first payment and 5 repayments`` () =
            let actual = monthlyParameters 110000L<Cent> 32<DurationDay> 5 |> calculateSchedule AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/ScheduledPaymentTest.Monthly062.md" (ValueSome 300))
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 153<OffsetDay>
                LevelPayment = 40666L<Cent>
                FinalPayment = 40666L<Cent>
                PaymentTotal = 203330L<Cent>
                PrincipalTotal = 110000L<Cent>
                InterestTotal = 93330L<Cent>
                Apr = Percent 1249.8m
                CostToBorrowingRatio = Percent 84.85m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£1300 with 04 days to first payment and 6 repayments`` () =
            let actual = monthlyParameters 130000L<Cent> 4<DurationDay> 6 |> calculateSchedule AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/ScheduledPaymentTest.Monthly007.md" (ValueSome 300))
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 156<OffsetDay>
                LevelPayment = 36040L<Cent>
                FinalPayment = 36037L<Cent>
                PaymentTotal = 216237L<Cent>
                PrincipalTotal = 130000L<Cent>
                InterestTotal = 86237L<Cent>
                Apr = Percent 1277.7m
                CostToBorrowingRatio = Percent 66.34m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£1300 with 08 days to first payment and 6 repayments`` () =
            let actual = monthlyParameters 130000L<Cent> 8<DurationDay> 6 |> calculateSchedule AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/ScheduledPaymentTest.Monthly015.md" (ValueSome 300))
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 160<OffsetDay>
                LevelPayment = 37154L<Cent>
                FinalPayment = 37158L<Cent>
                PaymentTotal = 222928L<Cent>
                PrincipalTotal = 130000L<Cent>
                InterestTotal = 92928L<Cent>
                Apr = Percent 1290.9m
                CostToBorrowingRatio = Percent 71.48m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£1300 with 12 days to first payment and 6 repayments`` () =
            let actual = monthlyParameters 130000L<Cent> 12<DurationDay> 6 |> calculateSchedule AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/ScheduledPaymentTest.Monthly023.md" (ValueSome 300))
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 164<OffsetDay>
                LevelPayment = 38269L<Cent>
                FinalPayment = 38274L<Cent>
                PaymentTotal = 229619L<Cent>
                PrincipalTotal = 130000L<Cent>
                InterestTotal = 99619L<Cent>
                Apr = Percent 1296.2m
                CostToBorrowingRatio = Percent 76.63m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£1300 with 16 days to first payment and 6 repayments`` () =
            let actual = monthlyParameters 130000L<Cent> 16<DurationDay> 6 |> calculateSchedule AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/ScheduledPaymentTest.Monthly031.md" (ValueSome 300))
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 168<OffsetDay>
                LevelPayment = 39384L<Cent>
                FinalPayment = 39386L<Cent>
                PaymentTotal = 236306L<Cent>
                PrincipalTotal = 130000L<Cent>
                InterestTotal = 106306L<Cent>
                Apr = Percent 1295m
                CostToBorrowingRatio = Percent 81.77m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£1300 with 20 days to first payment and 6 repayments`` () =
            let actual = monthlyParameters 130000L<Cent> 20<DurationDay> 6 |> calculateSchedule AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/ScheduledPaymentTest.Monthly039.md" (ValueSome 300))
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 172<OffsetDay>
                LevelPayment = 40499L<Cent>
                FinalPayment = 40497L<Cent>
                PaymentTotal = 242992L<Cent>
                PrincipalTotal = 130000L<Cent>
                InterestTotal = 112992L<Cent>
                Apr = Percent 1288.6m
                CostToBorrowingRatio = Percent 86.92m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£1300 with 24 days to first payment and 6 repayments`` () =
            let actual = monthlyParameters 130000L<Cent> 24<DurationDay> 6 |> calculateSchedule AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/ScheduledPaymentTest.Monthly047.md" (ValueSome 300))
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 176<OffsetDay>
                LevelPayment = 41491L<Cent>
                FinalPayment = 41490L<Cent>
                PaymentTotal = 248945L<Cent>
                PrincipalTotal = 130000L<Cent>
                InterestTotal = 118945L<Cent>
                Apr = Percent 1280.4m
                CostToBorrowingRatio = Percent 91.5m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£1300 with 28 days to first payment and 6 repayments`` () =
            let actual = monthlyParameters 130000L<Cent> 28<DurationDay> 6 |> calculateSchedule AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/ScheduledPaymentTest.Monthly055.md" (ValueSome 300))
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 180<OffsetDay>
                LevelPayment = 42602L<Cent>
                FinalPayment = 42604L<Cent>
                PaymentTotal = 255614L<Cent>
                PrincipalTotal = 130000L<Cent>
                InterestTotal = 125614L<Cent>
                Apr = Percent 1266.6m
                CostToBorrowingRatio = Percent 96.63m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£1300 with 32 days to first payment and 5 repayments`` () =
            let actual = monthlyParameters 130000L<Cent> 32<DurationDay> 5 |> calculateSchedule AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/ScheduledPaymentTest.Monthly063.md" (ValueSome 300))
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 153<OffsetDay>
                LevelPayment = 48060L<Cent>
                FinalPayment = 48058L<Cent>
                PaymentTotal = 240298L<Cent>
                PrincipalTotal = 130000L<Cent>
                InterestTotal = 110298L<Cent>
                Apr = Percent 1249.8m
                CostToBorrowingRatio = Percent 84.84m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£1500 with 04 days to first payment and 6 repayments`` () =
            let actual = monthlyParameters 150000L<Cent> 4<DurationDay> 6 |> calculateSchedule AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/ScheduledPaymentTest.Monthly008.md" (ValueSome 300))
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 156<OffsetDay>
                LevelPayment = 41584L<Cent>
                FinalPayment = 41586L<Cent>
                PaymentTotal = 249506L<Cent>
                PrincipalTotal = 150000L<Cent>
                InterestTotal = 99506L<Cent>
                Apr = Percent 1277.7m
                CostToBorrowingRatio = Percent 66.34m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£1500 with 08 days to first payment and 6 repayments`` () =
            let actual = monthlyParameters 150000L<Cent> 8<DurationDay> 6 |> calculateSchedule AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/ScheduledPaymentTest.Monthly016.md" (ValueSome 300))
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 160<OffsetDay>
                LevelPayment = 42871L<Cent>
                FinalPayment = 42865L<Cent>
                PaymentTotal = 257220L<Cent>
                PrincipalTotal = 150000L<Cent>
                InterestTotal = 107220L<Cent>
                Apr = Percent 1290.9m
                CostToBorrowingRatio = Percent 71.48m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£1500 with 12 days to first payment and 6 repayments`` () =
            let actual = monthlyParameters 150000L<Cent> 12<DurationDay> 6 |> calculateSchedule AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/ScheduledPaymentTest.Monthly024.md" (ValueSome 300))
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 164<OffsetDay>
                LevelPayment = 44157L<Cent>
                FinalPayment = 44157L<Cent>
                PaymentTotal = 264942L<Cent>
                PrincipalTotal = 150000L<Cent>
                InterestTotal = 114942L<Cent>
                Apr = Percent 1296.2m
                CostToBorrowingRatio = Percent 76.63m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£1500 with 16 days to first payment and 6 repayments`` () =
            let actual = monthlyParameters 150000L<Cent> 16<DurationDay> 6 |> calculateSchedule AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/ScheduledPaymentTest.Monthly032.md" (ValueSome 300))
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 168<OffsetDay>
                LevelPayment = 45443L<Cent>
                FinalPayment = 45445L<Cent>
                PaymentTotal = 272660L<Cent>
                PrincipalTotal = 150000L<Cent>
                InterestTotal = 122660L<Cent>
                Apr = Percent 1295m
                CostToBorrowingRatio = Percent 81.77m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£1500 with 20 days to first payment and 6 repayments`` () =
            let actual = monthlyParameters 150000L<Cent> 20<DurationDay> 6 |> calculateSchedule AboveZero //AroundZero finds negative principal balance first
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/ScheduledPaymentTest.Monthly040.md" (ValueSome 300))
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 172<OffsetDay>
                LevelPayment = 46729L<Cent>
                FinalPayment = 46733L<Cent>
                PaymentTotal = 280378L<Cent>
                PrincipalTotal = 150000L<Cent>
                InterestTotal = 130378L<Cent>
                Apr = Percent 1288.6m
                CostToBorrowingRatio = Percent 86.92m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£1500 with 24 days to first payment and 6 repayments`` () =
            let actual = monthlyParameters 150000L<Cent> 24<DurationDay> 6 |> calculateSchedule AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/ScheduledPaymentTest.Monthly048.md" (ValueSome 300))
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 176<OffsetDay>
                LevelPayment = 47874L<Cent>
                FinalPayment = 47876L<Cent>
                PaymentTotal = 287246L<Cent>
                PrincipalTotal = 150000L<Cent>
                InterestTotal = 137246L<Cent>
                Apr = Percent 1280.4m
                CostToBorrowingRatio = Percent 91.5m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£1500 with 28 days to first payment and 6 repayments`` () =
            let actual = monthlyParameters 150000L<Cent> 28<DurationDay> 6 |> calculateSchedule AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/ScheduledPaymentTest.Monthly056.md" (ValueSome 300))
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 180<OffsetDay>
                LevelPayment = 49157L<Cent>
                FinalPayment = 49152L<Cent>
                PaymentTotal = 294937L<Cent>
                PrincipalTotal = 150000L<Cent>
                InterestTotal = 144937L<Cent>
                Apr = Percent 1266.6m
                CostToBorrowingRatio = Percent 96.62m
            }
            actual |> should equal expected


        [<Fact>]
        let ``£1500 with 32 days to first payment and 5 repayments`` () =
            let actual = monthlyParameters 150000L<Cent> 32<DurationDay> 5 |> calculateSchedule AroundZero
            actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/ScheduledPaymentTest.Monthly064.md" (ValueSome 300))
            let expected = ValueSome {
                AsOfDay = 0<OffsetDay>
                Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
                FinalPaymentDay = 153<OffsetDay>
                LevelPayment = 55453L<Cent>
                FinalPayment = 55457L<Cent>
                PaymentTotal = 277269L<Cent>
                PrincipalTotal = 150000L<Cent>
                InterestTotal = 127269L<Cent>
                Apr = Percent 1249.8m
                CostToBorrowingRatio = Percent 84.85m
            }
            actual |> should equal expected


