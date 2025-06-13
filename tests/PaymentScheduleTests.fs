namespace FSharp.Finance.Personal.Tests

open System
open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal

module PaymentScheduleTests =

    let folder = "PaymentSchedule"

    open Amortisation
    open Calculation
    open DateDay
    open Formatting
    open Scheduling
    open UnitPeriod

    let interestCapExample: Interest.Cap = {
        TotalAmount = Amount.Percentage(Percent 100m, Restriction.NoLimit)
        DailyAmount = Amount.Percentage(Percent 0.8m, Restriction.NoLimit)
    }

    module Biweekly =
        let biweeklyParameters principal offset : BasicParameters =
            let startDate = Date(2023, 11, 15)

            {
                EvaluationDate = startDate
                StartDate = startDate
                Principal = principal
                ScheduleConfig =
                    AutoGenerateSchedule {
                        UnitPeriodConfig = Weekly(2u, startDate.AddDays(int offset))
                        ScheduleLength = PaymentCount 11u
                    }
                PaymentConfig = {
                    LevelPaymentOption = LowerFinalPayment
                    Rounding = RoundUp
                }
                FeeConfig =
                    ValueSome {
                        FeeType = Fee.FeeType.FacilitationFee(Amount.Percentage(Percent 189.47m, Restriction.NoLimit))
                        Rounding = RoundDown
                        FeeAmortisation = Fee.FeeAmortisation.AmortiseProportionately
                    }
                InterestConfig = {
                    Method = Interest.Method.Actuarial
                    StandardRate = Interest.Rate.Annual <| Percent 9.95m
                    Cap = Interest.Cap.zero
                    Rounding = RoundDown
                    AprMethod = Apr.CalculationMethod.UsActuarial
                    AprPrecision = 8u
                }
            }

        [<Fact>]
        let PaymentScheduleTest_Biweekly_1200_fp08_r11 () =
            let title = "PaymentScheduleTest_Biweekly_1200_fp08_r11"
            let p = biweeklyParameters 1200_00uL<Cent> 8u<OffsetDay>
            let actual = calculateBasicSchedule p

            actual
            |> BasicSchedule.outputHtmlToFile folder title "$1200 with short first period" p

            let expected = {
                EvaluationDay = 0u<OffsetDay>
                Items = actual.Items
                Stats = {
                    InitialInterestBalance = 0uL<Cent>
                    LastScheduledPaymentDay = 148u<OffsetDay>
                    LevelPayment = 322_53uL<Cent>
                    FinalPayment = 322_53uL<Cent>
                    ScheduledPaymentTotal = 3547_83uL<Cent>
                    PrincipalTotal = 3473_64uL<Cent>
                    InterestTotal = 74_19uL<Cent>
                    InitialApr = Percent 717.412507m
                    InitialCostToBorrowingRatio = Percent 67.59m
                }
            }

            actual |> should equal expected

        [<Fact>]
        let PaymentScheduleTest_Biweekly_1200_fp14_r11 () =
            let title = "PaymentScheduleTest_Biweekly_1200_fp14_r11"
            let description = "$1200 with first period equal to unit-period length"
            let p = biweeklyParameters 1200_00uL<Cent> 14u<OffsetDay>
            let actual = calculateBasicSchedule p
            actual |> BasicSchedule.outputHtmlToFile folder title description p

            let expected = {
                EvaluationDay = 0u<OffsetDay>
                Items = actual.Items
                Stats = {
                    InitialInterestBalance = 0uL<Cent>
                    LastScheduledPaymentDay = 154u<OffsetDay>
                    LevelPayment = 323_06uL<Cent>
                    FinalPayment = 323_03uL<Cent>
                    ScheduledPaymentTotal = 3553_63uL<Cent>
                    PrincipalTotal = 3473_64uL<Cent>
                    InterestTotal = 79_99uL<Cent>
                    InitialApr = Percent 637.159359m
                    InitialCostToBorrowingRatio = Percent 67.76m
                }
            }

            actual |> should equal expected

        [<Fact>]
        let PaymentScheduleTest_Biweekly_1200_fp15_r11 () =
            let title = "PaymentScheduleTest_Biweekly_1200_fp15_r11"
            let description = "$1200 with long first period"
            let p = biweeklyParameters 1200_00uL<Cent> 15u<OffsetDay>
            let actual = calculateBasicSchedule p
            actual |> BasicSchedule.outputHtmlToFile folder title description p

            let expected = {
                EvaluationDay = 0u<OffsetDay>
                Items = actual.Items
                Stats = {
                    InitialInterestBalance = 0uL<Cent>
                    LastScheduledPaymentDay = 155u<OffsetDay>
                    LevelPayment = 323_15uL<Cent>
                    FinalPayment = 323_10uL<Cent>
                    ScheduledPaymentTotal = 3554_60uL<Cent>
                    PrincipalTotal = 3473_64uL<Cent>
                    InterestTotal = 80_96uL<Cent>
                    InitialApr = Percent 623.703586m
                    InitialCostToBorrowingRatio = Percent 67.78m
                }
            }

            actual |> should equal expected

    module Monthly =

        let monthlyParameters principal offset paymentCount : BasicParameters =
            let startDate = Date(2023, 12, 07)

            {
                EvaluationDate = startDate
                StartDate = startDate
                Principal = principal
                ScheduleConfig =
                    AutoGenerateSchedule {
                        UnitPeriodConfig =
                            (startDate.AddDays(int offset)
                             |> fun d -> Monthly(1u, d.Year, d.Month, d.Day * 1))
                        ScheduleLength = PaymentCount paymentCount
                    }
                PaymentConfig = {
                    LevelPaymentOption = LowerFinalPayment
                    Rounding = RoundWith MidpointRounding.AwayFromZero
                }
                FeeConfig = ValueNone
                InterestConfig = {
                    Method = Interest.Method.Actuarial
                    StandardRate = Interest.Rate.Daily(Percent 0.798m)
                    Cap = interestCapExample
                    Rounding = RoundWith MidpointRounding.AwayFromZero
                    AprMethod = Apr.CalculationMethod.UnitedKingdom
                    AprPrecision = 3u
                }
            }

        [<Fact>]
        let PaymentScheduleTest_Monthly_0100_fp04_r5 () =
            let title = "PaymentScheduleTest_Monthly_0100_fp04_r5"
            let description = "£0100 with 04 days to first payment and 5 repayments"
            let p = monthlyParameters 100_00uL<Cent> 4u<OffsetDay> 5u
            let actual = calculateBasicSchedule p
            actual |> BasicSchedule.outputHtmlToFile folder title description p

            let expected = {
                EvaluationDay = 0u<OffsetDay>
                Items = actual.Items
                Stats = {
                    InitialInterestBalance = 0uL<Cent>
                    LastScheduledPaymentDay = 126u<OffsetDay>
                    LevelPayment = 30_49uL<Cent>
                    FinalPayment = 30_46uL<Cent>
                    ScheduledPaymentTotal = 152_42uL<Cent>
                    PrincipalTotal = 100_00uL<Cent>
                    InterestTotal = 52_42uL<Cent>
                    InitialApr = Percent 1280.8m
                    InitialCostToBorrowingRatio = Percent 52.42m
                }
            }

            actual |> should equal expected


        [<Fact>]
        let PaymentScheduleTest_Monthly_0100_fp08_r5 () =
            let title = "PaymentScheduleTest_Monthly_0100_fp08_r5"
            let description = "£0100 with 08 days to first payment and 5 repayments"
            let p = monthlyParameters 100_00uL<Cent> 8u<OffsetDay> 5u
            let actual = calculateBasicSchedule p
            actual |> BasicSchedule.outputHtmlToFile folder title description p

            let expected = {
                EvaluationDay = 0u<OffsetDay>
                Items = actual.Items
                Stats = {
                    InitialInterestBalance = 0uL<Cent>
                    LastScheduledPaymentDay = 130u<OffsetDay>
                    LevelPayment = 31_43uL<Cent>
                    FinalPayment = 31_42uL<Cent>
                    ScheduledPaymentTotal = 157_14uL<Cent>
                    PrincipalTotal = 100_00uL<Cent>
                    InterestTotal = 57_14uL<Cent>
                    InitialApr = Percent 1295.9m
                    InitialCostToBorrowingRatio = Percent 57.14m
                }
            }

            actual |> should equal expected


        [<Fact>]
        let PaymentScheduleTest_Monthly_0100_fp12_r4 () =
            let title = "PaymentScheduleTest_Monthly_0100_fp12_r4"
            let description = "£0100 with 12 days to first payment and 4 repayments"

            let p = {
                monthlyParameters 100_00uL<Cent> 12u<OffsetDay> 4u with
                    PaymentConfig.LevelPaymentOption = HigherFinalPayment
            }

            let actual = calculateBasicSchedule p
            actual |> BasicSchedule.outputHtmlToFile folder title description p

            let expected = {
                EvaluationDay = 0u<OffsetDay>
                Items = actual.Items
                Stats = {
                    InitialInterestBalance = 0uL<Cent>
                    LastScheduledPaymentDay = 103u<OffsetDay>
                    LevelPayment = 36_94uL<Cent>
                    FinalPayment = 36_95uL<Cent>
                    ScheduledPaymentTotal = 147_77uL<Cent>
                    PrincipalTotal = 100_00uL<Cent>
                    InterestTotal = 47_77uL<Cent>
                    InitialApr = Percent 1312.3m
                    InitialCostToBorrowingRatio = Percent 47.77m
                }
            }

            actual |> should equal expected


        [<Fact>]
        let PaymentScheduleTest_Monthly_0100_fp16_r4 () =
            let title = "PaymentScheduleTest_Monthly_0100_fp16_r4"
            let description = "£0100 with 16 days to first payment and 4 repayments"
            let p = monthlyParameters 100_00uL<Cent> 16u<OffsetDay> 4u
            let actual = calculateBasicSchedule p
            actual |> BasicSchedule.outputHtmlToFile folder title description p

            let expected = {
                EvaluationDay = 0u<OffsetDay>
                Items = actual.Items
                Stats = {
                    InitialInterestBalance = 0uL<Cent>
                    LastScheduledPaymentDay = 107u<OffsetDay>
                    LevelPayment = 38_02uL<Cent>
                    FinalPayment = 38_00uL<Cent>
                    ScheduledPaymentTotal = 152_06uL<Cent>
                    PrincipalTotal = 100_00uL<Cent>
                    InterestTotal = 52_06uL<Cent>
                    InitialApr = Percent 1309m
                    InitialCostToBorrowingRatio = Percent 52.06m
                }
            }

            actual |> should equal expected


        [<Fact>]
        let PaymentScheduleTest_Monthly_0100_fp20_r4 () =
            let title = "PaymentScheduleTest_Monthly_0100_fp20_r4"
            let description = "£0100 with 20 days to first payment and 4 repayments"

            let p = {
                monthlyParameters 100_00uL<Cent> 20u<OffsetDay> 4u with
                    PaymentConfig.LevelPaymentOption = SimilarFinalPayment
            }

            let actual = calculateBasicSchedule p
            actual |> BasicSchedule.outputHtmlToFile folder title description p

            let expected = {
                EvaluationDay = 0u<OffsetDay>
                Items = actual.Items
                Stats = {
                    InitialInterestBalance = 0uL<Cent>
                    LastScheduledPaymentDay = 111u<OffsetDay>
                    LevelPayment = 39_09uL<Cent>
                    FinalPayment = 39_11uL<Cent>
                    ScheduledPaymentTotal = 156_38uL<Cent>
                    PrincipalTotal = 100_00uL<Cent>
                    InterestTotal = 56_38uL<Cent>
                    InitialApr = Percent 1299.7m
                    InitialCostToBorrowingRatio = Percent 56.38m
                }
            }

            actual |> should equal expected


        [<Fact>]
        let PaymentScheduleTest_Monthly_0100_fp24_r4 () =
            let title = "PaymentScheduleTest_Monthly_0100_fp24_r4"
            let description = "£0100 with 24 days to first payment and 4 repayments"
            let p = monthlyParameters 100_00uL<Cent> 24u<OffsetDay> 4u
            let actual = calculateBasicSchedule p
            actual |> BasicSchedule.outputHtmlToFile folder title description p

            let expected = {
                EvaluationDay = 0u<OffsetDay>
                Items = actual.Items
                Stats = {
                    InitialInterestBalance = 0uL<Cent>
                    LastScheduledPaymentDay = 115u<OffsetDay>
                    LevelPayment = 40_06uL<Cent>
                    FinalPayment = 40_04uL<Cent>
                    ScheduledPaymentTotal = 160_22uL<Cent>
                    PrincipalTotal = 100_00uL<Cent>
                    InterestTotal = 60_22uL<Cent>
                    InitialApr = Percent 1287.7m
                    InitialCostToBorrowingRatio = Percent 60.22m
                }
            }

            actual |> should equal expected


        [<Fact>]
        let PaymentScheduleTest_Monthly_0100_fp28_r4 () =
            let title = "PaymentScheduleTest_Monthly_0100_fp28_r4"
            let description = "£0100 with 28 days to first payment and 4 repayments"
            let p = monthlyParameters 100_00uL<Cent> 28u<OffsetDay> 4u
            let actual = calculateBasicSchedule p
            actual |> BasicSchedule.outputHtmlToFile folder title description p

            let expected = {
                EvaluationDay = 0u<OffsetDay>
                Items = actual.Items
                Stats = {
                    InitialInterestBalance = 0uL<Cent>
                    LastScheduledPaymentDay = 119u<OffsetDay>
                    LevelPayment = 41_13uL<Cent>
                    FinalPayment = 41_11uL<Cent>
                    ScheduledPaymentTotal = 164_50uL<Cent>
                    PrincipalTotal = 100_00uL<Cent>
                    InterestTotal = 64_50uL<Cent>
                    InitialApr = Percent 1268.8m
                    InitialCostToBorrowingRatio = Percent 64.5m
                }
            }

            actual |> should equal expected


        [<Fact>]
        let PaymentScheduleTest_Monthly_0100_fp32_r4 () =
            let title = "PaymentScheduleTest_Monthly_0100_fp32_r4"
            let description = "£0100 with 32 days to first payment and 4 repayments"

            let p = {
                monthlyParameters 100_00uL<Cent> 32u<OffsetDay> 4u with
                    PaymentConfig.LevelPaymentOption = HigherFinalPayment
            }

            let actual = calculateBasicSchedule p
            actual |> BasicSchedule.outputHtmlToFile folder title description p

            let expected = {
                EvaluationDay = 0u<OffsetDay>
                Items = actual.Items
                Stats = {
                    InitialInterestBalance = 0uL<Cent>
                    LastScheduledPaymentDay = 123u<OffsetDay>
                    LevelPayment = 42_20uL<Cent>
                    FinalPayment = 42_22uL<Cent>
                    ScheduledPaymentTotal = 168_82uL<Cent>
                    PrincipalTotal = 100_00uL<Cent>
                    InterestTotal = 68_82uL<Cent>
                    InitialApr = Percent 1248.6m
                    InitialCostToBorrowingRatio = Percent 68.82m
                }
            }

            actual |> should equal expected


        [<Fact>]
        let PaymentScheduleTest_Monthly_0300_fp04_r5 () =
            let title = "PaymentScheduleTest_Monthly_0300_fp04_r5"
            let description = "£0300 with 04 days to first payment and 5 repayments"

            let p = {
                monthlyParameters 300_00uL<Cent> 4u<OffsetDay> 5u with
                    PaymentConfig.LevelPaymentOption = HigherFinalPayment
            }

            let actual = calculateBasicSchedule p
            actual |> BasicSchedule.outputHtmlToFile folder title description p

            let expected = {
                EvaluationDay = 0u<OffsetDay>
                Items = actual.Items
                Stats = {
                    InitialInterestBalance = 0uL<Cent>
                    LastScheduledPaymentDay = 126u<OffsetDay>
                    LevelPayment = 91_46uL<Cent>
                    FinalPayment = 91_50uL<Cent>
                    ScheduledPaymentTotal = 457_34uL<Cent>
                    PrincipalTotal = 300_00uL<Cent>
                    InterestTotal = 157_34uL<Cent>
                    InitialApr = Percent 1281.4m
                    InitialCostToBorrowingRatio = Percent 52.45m
                }
            }

            actual |> should equal expected


        [<Fact>]
        let PaymentScheduleTest_Monthly_0300_fp08_r5 () =
            let title = "PaymentScheduleTest_Monthly_0300_fp08_r5"
            let description = "£0300 with 08 days to first payment and 5 repayments"

            let p = {
                monthlyParameters 300_00uL<Cent> 8u<OffsetDay> 5u with
                    PaymentConfig.LevelPaymentOption = SimilarFinalPayment
            }

            let actual = calculateBasicSchedule p
            actual |> BasicSchedule.outputHtmlToFile folder title description p

            let expected = {
                EvaluationDay = 0u<OffsetDay>
                Items = actual.Items
                Stats = {
                    InitialInterestBalance = 0uL<Cent>
                    LastScheduledPaymentDay = 130u<OffsetDay>
                    LevelPayment = 94_29uL<Cent>
                    FinalPayment = 94_31uL<Cent>
                    ScheduledPaymentTotal = 471_47uL<Cent>
                    PrincipalTotal = 300_00uL<Cent>
                    InterestTotal = 171_47uL<Cent>
                    InitialApr = Percent 1296.5m
                    InitialCostToBorrowingRatio = Percent 57.16m
                }
            }

            actual |> should equal expected


        [<Fact>]
        let PaymentScheduleTest_Monthly_0300_fp12_r4 () =
            let title = "PaymentScheduleTest_Monthly_0300_fp12_r4"
            let description = "£0300 with 12 days to first payment and 4 repayments"

            let p = {
                monthlyParameters 300_00uL<Cent> 12u<OffsetDay> 4u with
                    PaymentConfig.LevelPaymentOption = SimilarFinalPayment
            }

            let actual = calculateBasicSchedule p
            actual |> BasicSchedule.outputHtmlToFile folder title description p

            let expected = {
                EvaluationDay = 0u<OffsetDay>
                Items = actual.Items
                Stats = {
                    InitialInterestBalance = 0uL<Cent>
                    LastScheduledPaymentDay = 103u<OffsetDay>
                    LevelPayment = 110_82uL<Cent>
                    FinalPayment = 110_84uL<Cent>
                    ScheduledPaymentTotal = 443_30uL<Cent>
                    PrincipalTotal = 300_00uL<Cent>
                    InterestTotal = 143_30uL<Cent>
                    InitialApr = Percent 1312.1m
                    InitialCostToBorrowingRatio = Percent 47.77m
                }
            }

            actual |> should equal expected


        [<Fact>]
        let PaymentScheduleTest_Monthly_0300_fp16_r4 () =
            let title = "PaymentScheduleTest_Monthly_0300_fp16_r4"
            let description = "£0300 with 16 days to first payment and 4 repayments"
            let p = monthlyParameters 300_00uL<Cent> 16u<OffsetDay> 4u
            let actual = calculateBasicSchedule p
            actual |> BasicSchedule.outputHtmlToFile folder title description p

            let expected = {
                EvaluationDay = 0u<OffsetDay>
                Items = actual.Items
                Stats = {
                    InitialInterestBalance = 0uL<Cent>
                    LastScheduledPaymentDay = 107u<OffsetDay>
                    LevelPayment = 114_05uL<Cent>
                    FinalPayment = 114_03uL<Cent>
                    ScheduledPaymentTotal = 456_18uL<Cent>
                    PrincipalTotal = 300_00uL<Cent>
                    InterestTotal = 156_18uL<Cent>
                    InitialApr = Percent 1308.8m
                    InitialCostToBorrowingRatio = Percent 52.06m
                }
            }

            actual |> should equal expected


        [<Fact>]
        let PaymentScheduleTest_Monthly_0300_fp20_r4 () =
            let title = "PaymentScheduleTest_Monthly_0300_fp20_r4"
            let description = "£0300 with 20 days to first payment and 4 repayments"
            let p = monthlyParameters 300_00uL<Cent> 20u<OffsetDay> 4u
            let actual = calculateBasicSchedule p
            actual |> BasicSchedule.outputHtmlToFile folder title description p

            let expected = {
                EvaluationDay = 0u<OffsetDay>
                Items = actual.Items
                Stats = {
                    InitialInterestBalance = 0uL<Cent>
                    LastScheduledPaymentDay = 111u<OffsetDay>
                    LevelPayment = 117_28uL<Cent>
                    FinalPayment = 117_28uL<Cent>
                    ScheduledPaymentTotal = 469_12uL<Cent>
                    PrincipalTotal = 300_00uL<Cent>
                    InterestTotal = 169_12uL<Cent>
                    InitialApr = Percent 1299.6m
                    InitialCostToBorrowingRatio = Percent 56.37m
                }
            }

            actual |> should equal expected


        [<Fact>]
        let PaymentScheduleTest_Monthly_0300_fp24_r4 () =
            let title = "PaymentScheduleTest_Monthly_0300_fp24_r4"
            let description = "£0300 with 24 days to first payment and 4 repayments"
            let p = monthlyParameters 300_00uL<Cent> 24u<OffsetDay> 4u
            let actual = calculateBasicSchedule p
            actual |> BasicSchedule.outputHtmlToFile folder title description p

            let expected = {
                EvaluationDay = 0u<OffsetDay>
                Items = actual.Items
                Stats = {
                    InitialInterestBalance = 0uL<Cent>
                    LastScheduledPaymentDay = 115u<OffsetDay>
                    LevelPayment = 120_17uL<Cent>
                    FinalPayment = 120_17uL<Cent>
                    ScheduledPaymentTotal = 480_68uL<Cent>
                    PrincipalTotal = 300_00uL<Cent>
                    InterestTotal = 180_68uL<Cent>
                    InitialApr = Percent 1287.8m
                    InitialCostToBorrowingRatio = Percent 60.23m
                }
            }

            actual |> should equal expected


        [<Fact>]
        let PaymentScheduleTest_Monthly_0300_fp28_r4 () =
            let title = "PaymentScheduleTest_Monthly_0300_fp28_r4"
            let description = "£0300 with 28 days to first payment and 4 repayments"
            let p = monthlyParameters 300_00uL<Cent> 28u<OffsetDay> 4u
            let actual = calculateBasicSchedule p
            actual |> BasicSchedule.outputHtmlToFile folder title description p

            let expected = {
                EvaluationDay = 0u<OffsetDay>
                Items = actual.Items
                Stats = {
                    InitialInterestBalance = 0uL<Cent>
                    LastScheduledPaymentDay = 119u<OffsetDay>
                    LevelPayment = 123_39uL<Cent>
                    FinalPayment = 123_38uL<Cent>
                    ScheduledPaymentTotal = 493_55uL<Cent>
                    PrincipalTotal = 300_00uL<Cent>
                    InterestTotal = 193_55uL<Cent>
                    InitialApr = Percent 1269.3m
                    InitialCostToBorrowingRatio = Percent 64.52m
                }
            }

            actual |> should equal expected


        [<Fact>]
        let PaymentScheduleTest_Monthly_0300_fp32_r4 () =
            let title = "PaymentScheduleTest_Monthly_0300_fp32_r4"
            let description = "£0300 with 32 days to first payment and 4 repayments"
            let p = monthlyParameters 300_00uL<Cent> 32u<OffsetDay> 4u
            let actual = calculateBasicSchedule p
            actual |> BasicSchedule.outputHtmlToFile folder title description p

            let expected = {
                EvaluationDay = 0u<OffsetDay>
                Items = actual.Items
                Stats = {
                    InitialInterestBalance = 0uL<Cent>
                    LastScheduledPaymentDay = 123u<OffsetDay>
                    LevelPayment = 126_61uL<Cent>
                    FinalPayment = 126_61uL<Cent>
                    ScheduledPaymentTotal = 506_44uL<Cent>
                    PrincipalTotal = 300_00uL<Cent>
                    InterestTotal = 206_44uL<Cent>
                    InitialApr = Percent 1248.6m
                    InitialCostToBorrowingRatio = Percent 68.81m
                }
            }

            actual |> should equal expected


        [<Fact>]
        let PaymentScheduleTest_Monthly_0500_fp04_r5 () =
            let title = "PaymentScheduleTest_Monthly_0500_fp04_r5"
            let description = "£0500 with 04 days to first payment and 5 repayments"
            let p = monthlyParameters 500_00uL<Cent> 4u<OffsetDay> 5u
            let actual = calculateBasicSchedule p
            actual |> BasicSchedule.outputHtmlToFile folder title description p

            let expected = {
                EvaluationDay = 0u<OffsetDay>
                Items = actual.Items
                Stats = {
                    InitialInterestBalance = 0uL<Cent>
                    LastScheduledPaymentDay = 126u<OffsetDay>
                    LevelPayment = 152_44uL<Cent>
                    FinalPayment = 152_43uL<Cent>
                    ScheduledPaymentTotal = 762_19uL<Cent>
                    PrincipalTotal = 500_00uL<Cent>
                    InterestTotal = 262_19uL<Cent>
                    InitialApr = Percent 1281.2m
                    InitialCostToBorrowingRatio = Percent 52.44m
                }
            }

            actual |> should equal expected


        [<Fact>]
        let PaymentScheduleTest_Monthly_0500_fp08_r5 () =
            let title = "PaymentScheduleTest_Monthly_0500_fp08_r5"
            let description = "£0500 with 08 days to first payment and 5 repayments"
            let p = monthlyParameters 500_00uL<Cent> 8u<OffsetDay> 5u
            let actual = calculateBasicSchedule p
            actual |> BasicSchedule.outputHtmlToFile folder title description p

            let expected = {
                EvaluationDay = 0u<OffsetDay>
                Items = actual.Items
                Stats = {
                    InitialInterestBalance = 0uL<Cent>
                    LastScheduledPaymentDay = 130u<OffsetDay>
                    LevelPayment = 157_16uL<Cent>
                    FinalPayment = 157_12uL<Cent>
                    ScheduledPaymentTotal = 785_76uL<Cent>
                    PrincipalTotal = 500_00uL<Cent>
                    InterestTotal = 285_76uL<Cent>
                    InitialApr = Percent 1296.6m
                    InitialCostToBorrowingRatio = Percent 57.15m
                }
            }

            actual |> should equal expected


        [<Fact>]
        let PaymentScheduleTest_Monthly_0500_fp12_r4 () =
            let title = "PaymentScheduleTest_Monthly_0500_fp12_r4"
            let description = "£0500 with 12 days to first payment and 4 repayments"

            let p = {
                monthlyParameters 500_00uL<Cent> 12u<OffsetDay> 4u with
                    PaymentConfig.LevelPaymentOption = SimilarFinalPayment
            }

            let actual = calculateBasicSchedule p
            actual |> BasicSchedule.outputHtmlToFile folder title description p

            let expected = {
                EvaluationDay = 0u<OffsetDay>
                Items = actual.Items
                Stats = {
                    InitialInterestBalance = 0uL<Cent>
                    LastScheduledPaymentDay = 103u<OffsetDay>
                    LevelPayment = 184_70uL<Cent>
                    FinalPayment = 184_71uL<Cent>
                    ScheduledPaymentTotal = 738_81uL<Cent>
                    PrincipalTotal = 500_00uL<Cent>
                    InterestTotal = 238_81uL<Cent>
                    InitialApr = Percent 1311.9m
                    InitialCostToBorrowingRatio = Percent 47.76m
                }
            }

            actual |> should equal expected


        [<Fact>]
        let PaymentScheduleTest_Monthly_0500_fp16_r4 () =
            let title = "PaymentScheduleTest_Monthly_0500_fp16_r4"
            let description = "£0500 with 16 days to first payment and 4 repayments"

            let p = {
                monthlyParameters 500_00uL<Cent> 16u<OffsetDay> 4u with
                    PaymentConfig.LevelPaymentOption = HigherFinalPayment
            }

            let actual = calculateBasicSchedule p
            actual |> BasicSchedule.outputHtmlToFile folder title description p

            let expected = {
                EvaluationDay = 0u<OffsetDay>
                Items = actual.Items
                Stats = {
                    InitialInterestBalance = 0uL<Cent>
                    LastScheduledPaymentDay = 107u<OffsetDay>
                    LevelPayment = 190_08uL<Cent>
                    FinalPayment = 190_09uL<Cent>
                    ScheduledPaymentTotal = 760_33uL<Cent>
                    PrincipalTotal = 500_00uL<Cent>
                    InterestTotal = 260_33uL<Cent>
                    InitialApr = Percent 1309m
                    InitialCostToBorrowingRatio = Percent 52.07m
                }
            }

            actual |> should equal expected


        [<Fact>]
        let PaymentScheduleTest_Monthly_0500_fp20_r4 () =
            let title = "PaymentScheduleTest_Monthly_0500_fp20_r4"
            let description = "£0500 with 20 days to first payment and 4 repayments"
            let p = monthlyParameters 500_00uL<Cent> 20u<OffsetDay> 4u
            let actual = calculateBasicSchedule p
            actual |> BasicSchedule.outputHtmlToFile folder title description p

            let expected = {
                EvaluationDay = 0u<OffsetDay>
                Items = actual.Items
                Stats = {
                    InitialInterestBalance = 0uL<Cent>
                    LastScheduledPaymentDay = 111u<OffsetDay>
                    LevelPayment = 195_47uL<Cent>
                    FinalPayment = 195_44uL<Cent>
                    ScheduledPaymentTotal = 781_85uL<Cent>
                    PrincipalTotal = 500_00uL<Cent>
                    InterestTotal = 281_85uL<Cent>
                    InitialApr = Percent 1299.5m
                    InitialCostToBorrowingRatio = Percent 56.37m
                }
            }

            actual |> should equal expected


        [<Fact>]
        let PaymentScheduleTest_Monthly_0500_fp24_r4 () =
            let title = "PaymentScheduleTest_Monthly_0500_fp24_r4"
            let description = "£0500 with 24 days to first payment and 4 repayments"
            let p = monthlyParameters 500_00uL<Cent> 24u<OffsetDay> 4u
            let actual = calculateBasicSchedule p
            actual |> BasicSchedule.outputHtmlToFile folder title description p

            let expected = {
                EvaluationDay = 0u<OffsetDay>
                Items = actual.Items
                Stats = {
                    InitialInterestBalance = 0uL<Cent>
                    LastScheduledPaymentDay = 115u<OffsetDay>
                    LevelPayment = 200_28uL<Cent>
                    FinalPayment = 200_28uL<Cent>
                    ScheduledPaymentTotal = 801_12uL<Cent>
                    PrincipalTotal = 500_00uL<Cent>
                    InterestTotal = 301_12uL<Cent>
                    InitialApr = Percent 1287.6m
                    InitialCostToBorrowingRatio = Percent 60.22m
                }
            }

            actual |> should equal expected


        [<Fact>]
        let PaymentScheduleTest_Monthly_0500_fp28_r4 () =
            let title = "PaymentScheduleTest_Monthly_0500_fp28_r4"
            let description = "£0500 with 28 days to first payment and 4 repayments"
            let p = monthlyParameters 500_00uL<Cent> 28u<OffsetDay> 4u
            let actual = calculateBasicSchedule p
            actual |> BasicSchedule.outputHtmlToFile folder title description p

            let expected = {
                EvaluationDay = 0u<OffsetDay>
                Items = actual.Items
                Stats = {
                    InitialInterestBalance = 0uL<Cent>
                    LastScheduledPaymentDay = 119u<OffsetDay>
                    LevelPayment = 205_65uL<Cent>
                    FinalPayment = 205_63uL<Cent>
                    ScheduledPaymentTotal = 822_58uL<Cent>
                    PrincipalTotal = 500_00uL<Cent>
                    InterestTotal = 322_58uL<Cent>
                    InitialApr = Percent 1269.3m
                    InitialCostToBorrowingRatio = Percent 64.52m
                }
            }

            actual |> should equal expected


        [<Fact>]
        let PaymentScheduleTest_Monthly_0500_fp32_r4 () =
            let title = "PaymentScheduleTest_Monthly_0500_fp32_r4"
            let description = "£0500 with 32 days to first payment and 4 repayments"

            let p = {
                monthlyParameters 500_00uL<Cent> 32u<OffsetDay> 4u with
                    PaymentConfig.LevelPaymentOption = SimilarFinalPayment
            }

            let actual = calculateBasicSchedule p
            actual |> BasicSchedule.outputHtmlToFile folder title description p

            let expected = {
                EvaluationDay = 0u<OffsetDay>
                Items = actual.Items
                Stats = {
                    InitialInterestBalance = 0uL<Cent>
                    LastScheduledPaymentDay = 123u<OffsetDay>
                    LevelPayment = 211_01uL<Cent>
                    FinalPayment = 211_03uL<Cent>
                    ScheduledPaymentTotal = 844_06uL<Cent>
                    PrincipalTotal = 500_00uL<Cent>
                    InterestTotal = 344_06uL<Cent>
                    InitialApr = Percent 1248.5m
                    InitialCostToBorrowingRatio = Percent 68.81m
                }
            }

            actual |> should equal expected


        [<Fact>]
        let PaymentScheduleTest_Monthly_0700_fp04_r5 () =
            let title = "PaymentScheduleTest_Monthly_0700_fp04_r5"
            let description = "£0700 with 04 days to first payment and 5 repayments"
            let p = monthlyParameters 700_00uL<Cent> 4u<OffsetDay> 5u
            let actual = calculateBasicSchedule p
            actual |> BasicSchedule.outputHtmlToFile folder title description p

            let expected = {
                EvaluationDay = 0u<OffsetDay>
                Items = actual.Items
                Stats = {
                    InitialInterestBalance = 0uL<Cent>
                    LastScheduledPaymentDay = 126u<OffsetDay>
                    LevelPayment = 213_42uL<Cent>
                    FinalPayment = 213_39uL<Cent>
                    ScheduledPaymentTotal = 1067_07uL<Cent>
                    PrincipalTotal = 700_00uL<Cent>
                    InterestTotal = 367_07uL<Cent>
                    InitialApr = Percent 1281.3m
                    InitialCostToBorrowingRatio = Percent 52.44m
                }
            }

            actual |> should equal expected


        [<Fact>]
        let PaymentScheduleTest_Monthly_0700_fp08_r5 () =
            let title = "PaymentScheduleTest_Monthly_0700_fp08_r5"
            let description = "£0700 with 08 days to first payment and 5 repayments"
            let p = monthlyParameters 700_00uL<Cent> 8u<OffsetDay> 5u
            let actual = calculateBasicSchedule p
            actual |> BasicSchedule.outputHtmlToFile folder title description p

            let expected = {
                EvaluationDay = 0u<OffsetDay>
                Items = actual.Items
                Stats = {
                    InitialInterestBalance = 0uL<Cent>
                    LastScheduledPaymentDay = 130u<OffsetDay>
                    LevelPayment = 220_02uL<Cent>
                    FinalPayment = 219_99uL<Cent>
                    ScheduledPaymentTotal = 1100_07uL<Cent>
                    PrincipalTotal = 700_00uL<Cent>
                    InterestTotal = 400_07uL<Cent>
                    InitialApr = Percent 1296.5m
                    InitialCostToBorrowingRatio = Percent 57.15m
                }
            }

            actual |> should equal expected


        [<Fact>]
        let PaymentScheduleTest_Monthly_0700_fp12_r4 () =
            let title = "PaymentScheduleTest_Monthly_0700_fp12_r4"
            let description = "£0700 with 12 days to first payment and 4 repayments"
            let p = monthlyParameters 700_00uL<Cent> 12u<OffsetDay> 4u
            let actual = calculateBasicSchedule p
            actual |> BasicSchedule.outputHtmlToFile folder title description p

            let expected = {
                EvaluationDay = 0u<OffsetDay>
                Items = actual.Items
                Stats = {
                    InitialInterestBalance = 0uL<Cent>
                    LastScheduledPaymentDay = 103u<OffsetDay>
                    LevelPayment = 258_59uL<Cent>
                    FinalPayment = 258_55uL<Cent>
                    ScheduledPaymentTotal = 1034_32uL<Cent>
                    PrincipalTotal = 700_00uL<Cent>
                    InterestTotal = 334_32uL<Cent>
                    InitialApr = Percent 1311.9m
                    InitialCostToBorrowingRatio = Percent 47.76m
                }
            }

            actual |> should equal expected


        [<Fact>]
        let PaymentScheduleTest_Monthly_0700_fp16_r4 () =
            let title = "PaymentScheduleTest_Monthly_0700_fp16_r4"
            let description = "£0700 with 16 days to first payment and 4 repayments"
            let p = monthlyParameters 700_00uL<Cent> 16u<OffsetDay> 4u
            let actual = calculateBasicSchedule p
            actual |> BasicSchedule.outputHtmlToFile folder title description p

            let expected = {
                EvaluationDay = 0u<OffsetDay>
                Items = actual.Items
                Stats = {
                    InitialInterestBalance = 0uL<Cent>
                    LastScheduledPaymentDay = 107u<OffsetDay>
                    LevelPayment = 266_12uL<Cent>
                    FinalPayment = 266_10uL<Cent>
                    ScheduledPaymentTotal = 1064_46uL<Cent>
                    PrincipalTotal = 700_00uL<Cent>
                    InterestTotal = 364_46uL<Cent>
                    InitialApr = Percent 1309.1m
                    InitialCostToBorrowingRatio = Percent 52.07m
                }
            }

            actual |> should equal expected


        [<Fact>]
        let PaymentScheduleTest_Monthly_0700_fp20_r4 () =
            let title = "PaymentScheduleTest_Monthly_0700_fp20_r4"
            let description = "£0700 with 20 days to first payment and 4 repayments"
            let p = monthlyParameters 700_00uL<Cent> 20u<OffsetDay> 4u
            let actual = calculateBasicSchedule p
            actual |> BasicSchedule.outputHtmlToFile folder title description p

            let expected = {
                EvaluationDay = 0u<OffsetDay>
                Items = actual.Items
                Stats = {
                    InitialInterestBalance = 0uL<Cent>
                    LastScheduledPaymentDay = 111u<OffsetDay>
                    LevelPayment = 273_65uL<Cent>
                    FinalPayment = 273_65uL<Cent>
                    ScheduledPaymentTotal = 1094_60uL<Cent>
                    PrincipalTotal = 700_00uL<Cent>
                    InterestTotal = 394_60uL<Cent>
                    InitialApr = Percent 1299.5m
                    InitialCostToBorrowingRatio = Percent 56.37m
                }
            }

            actual |> should equal expected


        [<Fact>]
        let PaymentScheduleTest_Monthly_0700_fp24_r4 () =
            let title = "PaymentScheduleTest_Monthly_0700_fp24_r4"
            let description = "£0700 with 24 days to first payment and 4 repayments"

            let p = {
                monthlyParameters 700_00uL<Cent> 24u<OffsetDay> 4u with
                    PaymentConfig.LevelPaymentOption = SimilarFinalPayment
            }

            let actual = calculateBasicSchedule p
            actual |> BasicSchedule.outputHtmlToFile folder title description p

            let expected = {
                EvaluationDay = 0u<OffsetDay>
                Items = actual.Items
                Stats = {
                    InitialInterestBalance = 0uL<Cent>
                    LastScheduledPaymentDay = 115u<OffsetDay>
                    LevelPayment = 280_39uL<Cent>
                    FinalPayment = 280_41uL<Cent>
                    ScheduledPaymentTotal = 1121_58uL<Cent>
                    PrincipalTotal = 700_00uL<Cent>
                    InterestTotal = 421_58uL<Cent>
                    InitialApr = Percent 1287.7m
                    InitialCostToBorrowingRatio = Percent 60.23m
                }
            }

            actual |> should equal expected


        [<Fact>]
        let PaymentScheduleTest_Monthly_0700_fp28_r4 () =
            let title = "PaymentScheduleTest_Monthly_0700_fp28_r4"
            let description = "£0700 with 28 days to first payment and 4 repayments"
            let p = monthlyParameters 700_00uL<Cent> 28u<OffsetDay> 4u
            let actual = calculateBasicSchedule p
            actual |> BasicSchedule.outputHtmlToFile folder title description p

            let expected = {
                EvaluationDay = 0u<OffsetDay>
                Items = actual.Items
                Stats = {
                    InitialInterestBalance = 0uL<Cent>
                    LastScheduledPaymentDay = 119u<OffsetDay>
                    LevelPayment = 287_91uL<Cent>
                    FinalPayment = 287_90uL<Cent>
                    ScheduledPaymentTotal = 1151_63uL<Cent>
                    PrincipalTotal = 700_00uL<Cent>
                    InterestTotal = 451_63uL<Cent>
                    InitialApr = Percent 1269.4m
                    InitialCostToBorrowingRatio = Percent 64.52m
                }
            }

            actual |> should equal expected


        [<Fact>]
        let PaymentScheduleTest_Monthly_0700_fp32_r4 () =
            let title = "PaymentScheduleTest_Monthly_0700_fp32_r4"
            let description = "£0700 with 32 days to first payment and 4 repayments"
            let p = monthlyParameters 700_00uL<Cent> 32u<OffsetDay> 4u
            let actual = calculateBasicSchedule p
            actual |> BasicSchedule.outputHtmlToFile folder title description p

            let expected = {
                EvaluationDay = 0u<OffsetDay>
                Items = actual.Items
                Stats = {
                    InitialInterestBalance = 0uL<Cent>
                    LastScheduledPaymentDay = 123u<OffsetDay>
                    LevelPayment = 295_42uL<Cent>
                    FinalPayment = 295_39uL<Cent>
                    ScheduledPaymentTotal = 1181_65uL<Cent>
                    PrincipalTotal = 700_00uL<Cent>
                    InterestTotal = 481_65uL<Cent>
                    InitialApr = Percent 1248.4m
                    InitialCostToBorrowingRatio = Percent 68.81m
                }
            }

            actual |> should equal expected


        [<Fact>]
        let PaymentScheduleTest_Monthly_0900_fp04_r6 () =
            let title = "PaymentScheduleTest_Monthly_0900_fp04_r6"
            let description = "£0900 with 04 days to first payment and 6 repayments"
            let p = monthlyParameters 900_00uL<Cent> 4u<OffsetDay> 6u
            let actual = calculateBasicSchedule p
            actual |> BasicSchedule.outputHtmlToFile folder title description p

            let expected = {
                EvaluationDay = 0u<OffsetDay>
                Items = actual.Items
                Stats = {
                    InitialInterestBalance = 0uL<Cent>
                    LastScheduledPaymentDay = 156u<OffsetDay>
                    LevelPayment = 249_51uL<Cent>
                    FinalPayment = 249_48uL<Cent>
                    ScheduledPaymentTotal = 1497_03uL<Cent>
                    PrincipalTotal = 900_00uL<Cent>
                    InterestTotal = 597_03uL<Cent>
                    InitialApr = Percent 1277.8m
                    InitialCostToBorrowingRatio = Percent 66.34m
                }
            }

            actual |> should equal expected


        [<Fact>]
        let PaymentScheduleTest_Monthly_0900_fp08_r6 () =
            let title = "PaymentScheduleTest_Monthly_0900_fp08_r6"
            let description = "£0900 with 08 days to first payment and 6 repayments"

            let p = {
                monthlyParameters 900_00uL<Cent> 8u<OffsetDay> 6u with
                    PaymentConfig.LevelPaymentOption = HigherFinalPayment
            }

            let actual = calculateBasicSchedule p
            actual |> BasicSchedule.outputHtmlToFile folder title description p

            let expected = {
                EvaluationDay = 0u<OffsetDay>
                Items = actual.Items
                Stats = {
                    InitialInterestBalance = 0uL<Cent>
                    LastScheduledPaymentDay = 160u<OffsetDay>
                    LevelPayment = 257_22uL<Cent>
                    FinalPayment = 257_29uL<Cent>
                    ScheduledPaymentTotal = 1543_39uL<Cent>
                    PrincipalTotal = 900_00uL<Cent>
                    InterestTotal = 643_39uL<Cent>
                    InitialApr = Percent 1291.1m
                    InitialCostToBorrowingRatio = Percent 71.49m
                }
            }

            actual |> should equal expected


        [<Fact>]
        let PaymentScheduleTest_Monthly_0900_fp12_r6 () =
            let title = "PaymentScheduleTest_Monthly_0900_fp12_r6"
            let description = "£0900 with 12 days to first payment and 6 repayments"

            let p = {
                monthlyParameters 900_00uL<Cent> 12u<OffsetDay> 6u with
                    PaymentConfig.LevelPaymentOption = SimilarFinalPayment
            }

            let actual = calculateBasicSchedule p
            actual |> BasicSchedule.outputHtmlToFile folder title description p

            let expected = {
                EvaluationDay = 0u<OffsetDay>
                Items = actual.Items
                Stats = {
                    InitialInterestBalance = 0uL<Cent>
                    LastScheduledPaymentDay = 164u<OffsetDay>
                    LevelPayment = 264_94uL<Cent>
                    FinalPayment = 264_95uL<Cent>
                    ScheduledPaymentTotal = 1589_65uL<Cent>
                    PrincipalTotal = 900_00uL<Cent>
                    InterestTotal = 689_65uL<Cent>
                    InitialApr = Percent 1296.2m
                    InitialCostToBorrowingRatio = Percent 76.63m
                }
            }

            actual |> should equal expected


        [<Fact>]
        let PaymentScheduleTest_Monthly_0900_fp16_r6 () =
            let title = "PaymentScheduleTest_Monthly_0900_fp16_r6"
            let description = "£0900 with 16 days to first payment and 6 repayments"
            let p = monthlyParameters 900_00uL<Cent> 16u<OffsetDay> 6u
            let actual = calculateBasicSchedule p
            actual |> BasicSchedule.outputHtmlToFile folder title description p

            let expected = {
                EvaluationDay = 0u<OffsetDay>
                Items = actual.Items
                Stats = {
                    InitialInterestBalance = 0uL<Cent>
                    LastScheduledPaymentDay = 168u<OffsetDay>
                    LevelPayment = 272_66uL<Cent>
                    FinalPayment = 272_64uL<Cent>
                    ScheduledPaymentTotal = 1635_94uL<Cent>
                    PrincipalTotal = 900_00uL<Cent>
                    InterestTotal = 735_94uL<Cent>
                    InitialApr = Percent 1294.9m
                    InitialCostToBorrowingRatio = Percent 81.77m
                }
            }

            actual |> should equal expected


        [<Fact>]
        let PaymentScheduleTest_Monthly_0900_fp20_r5 () =
            let title = "PaymentScheduleTest_Monthly_0900_fp20_r5"
            let description = "£0900 with 20 days to first payment and 5 repayments"

            let p = {
                monthlyParameters 900_00uL<Cent> 20u<OffsetDay> 5u with
                    PaymentConfig.LevelPaymentOption = SimilarFinalPayment
            }

            let actual = calculateBasicSchedule p
            actual |> BasicSchedule.outputHtmlToFile folder title description p

            let expected = {
                EvaluationDay = 0u<OffsetDay>
                Items = actual.Items
                Stats = {
                    InitialInterestBalance = 0uL<Cent>
                    LastScheduledPaymentDay = 142u<OffsetDay>
                    LevelPayment = 308_34uL<Cent>
                    FinalPayment = 308_35uL<Cent>
                    ScheduledPaymentTotal = 1541_71uL<Cent>
                    PrincipalTotal = 900_00uL<Cent>
                    InterestTotal = 641_71uL<Cent>
                    InitialApr = Percent 1292.9m
                    InitialCostToBorrowingRatio = Percent 71.3m
                }
            }

            actual |> should equal expected


        [<Fact>]
        let PaymentScheduleTest_Monthly_0900_fp24_r5 () =
            let title = "PaymentScheduleTest_Monthly_0900_fp24_r5"
            let description = "£0900 with 24 days to first payment and 5 repayments"

            let p = {
                monthlyParameters 900_00uL<Cent> 24u<OffsetDay> 5u with
                    PaymentConfig.LevelPaymentOption = SimilarFinalPayment
            }

            let actual = calculateBasicSchedule p
            actual |> BasicSchedule.outputHtmlToFile folder title description p

            let expected = {
                EvaluationDay = 0u<OffsetDay>
                Items = actual.Items
                Stats = {
                    InitialInterestBalance = 0uL<Cent>
                    LastScheduledPaymentDay = 145u<OffsetDay>
                    LevelPayment = 315_80uL<Cent>
                    FinalPayment = 315_81uL<Cent>
                    ScheduledPaymentTotal = 1579_01uL<Cent>
                    PrincipalTotal = 900_00uL<Cent>
                    InterestTotal = 679_01uL<Cent>
                    InitialApr = Percent 1283.5m
                    InitialCostToBorrowingRatio = Percent 75.45m
                }
            }

            actual |> should equal expected


        [<Fact>]
        let PaymentScheduleTest_Monthly_0900_fp28_r5 () =
            let title = "PaymentScheduleTest_Monthly_0900_fp28_r5"
            let description = "£0900 with 28 days to first payment and 5 repayments"
            let p = monthlyParameters 900_00uL<Cent> 28u<OffsetDay> 5u
            let actual = calculateBasicSchedule p
            actual |> BasicSchedule.outputHtmlToFile folder title description p

            let expected = {
                EvaluationDay = 0u<OffsetDay>
                Items = actual.Items
                Stats = {
                    InitialInterestBalance = 0uL<Cent>
                    LastScheduledPaymentDay = 149u<OffsetDay>
                    LevelPayment = 324_26uL<Cent>
                    FinalPayment = 324_26uL<Cent>
                    ScheduledPaymentTotal = 1621_30uL<Cent>
                    PrincipalTotal = 900_00uL<Cent>
                    InterestTotal = 721_30uL<Cent>
                    InitialApr = Percent 1267.9m
                    InitialCostToBorrowingRatio = Percent 80.14m
                }
            }

            actual |> should equal expected


        [<Fact>]
        let PaymentScheduleTest_Monthly_0900_fp32_r5 () =
            let title = "PaymentScheduleTest_Monthly_0900_fp32_r5"
            let description = "£0900 with 32 days to first payment and 5 repayments"
            let p = monthlyParameters 900_00uL<Cent> 32u<OffsetDay> 5u
            let actual = calculateBasicSchedule p
            actual |> BasicSchedule.outputHtmlToFile folder title description p

            let expected = {
                EvaluationDay = 0u<OffsetDay>
                Items = actual.Items
                Stats = {
                    InitialInterestBalance = 0uL<Cent>
                    LastScheduledPaymentDay = 153u<OffsetDay>
                    LevelPayment = 332_72uL<Cent>
                    FinalPayment = 332_72uL<Cent>
                    ScheduledPaymentTotal = 1663_60uL<Cent>
                    PrincipalTotal = 900_00uL<Cent>
                    InterestTotal = 763_60uL<Cent>
                    InitialApr = Percent 1249.8m
                    InitialCostToBorrowingRatio = Percent 84.84m
                }
            }

            actual |> should equal expected


        [<Fact>]
        let PaymentScheduleTest_Monthly_1100_fp04_r6 () =
            let title = "PaymentScheduleTest_Monthly_1100_fp04_r6"
            let description = "£1100 with 04 days to first payment and 6 repayments"
            let p = monthlyParameters 1100_00uL<Cent> 4u<OffsetDay> 6u
            let actual = calculateBasicSchedule p
            actual |> BasicSchedule.outputHtmlToFile folder title description p

            let expected = {
                EvaluationDay = 0u<OffsetDay>
                Items = actual.Items
                Stats = {
                    InitialInterestBalance = 0uL<Cent>
                    LastScheduledPaymentDay = 156u<OffsetDay>
                    LevelPayment = 304_95uL<Cent>
                    FinalPayment = 304_94uL<Cent>
                    ScheduledPaymentTotal = 1829_69uL<Cent>
                    PrincipalTotal = 1100_00uL<Cent>
                    InterestTotal = 729_69uL<Cent>
                    InitialApr = Percent 1277.7m
                    InitialCostToBorrowingRatio = Percent 66.34m
                }
            }

            actual |> should equal expected


        [<Fact>]
        let PaymentScheduleTest_Monthly_1100_fp08_r6 () =
            let title = "PaymentScheduleTest_Monthly_1100_fp08_r6"
            let description = "£1100 with 08 days to first payment and 6 repayments"
            let p = monthlyParameters 1100_00uL<Cent> 8u<OffsetDay> 6u
            let actual = calculateBasicSchedule p
            actual |> BasicSchedule.outputHtmlToFile folder title description p

            let expected = {
                EvaluationDay = 0u<OffsetDay>
                Items = actual.Items
                Stats = {
                    InitialInterestBalance = 0uL<Cent>
                    LastScheduledPaymentDay = 160u<OffsetDay>
                    LevelPayment = 314_39uL<Cent>
                    FinalPayment = 314_34uL<Cent>
                    ScheduledPaymentTotal = 1886_29uL<Cent>
                    PrincipalTotal = 1100_00uL<Cent>
                    InterestTotal = 786_29uL<Cent>
                    InitialApr = Percent 1291m
                    InitialCostToBorrowingRatio = Percent 71.48m
                }
            }

            actual |> should equal expected


        [<Fact>]
        let PaymentScheduleTest_Monthly_1100_fp12_r6 () =
            let title = "PaymentScheduleTest_Monthly_1100_fp12_r6"
            let description = "£1100 with 12 days to first payment and 6 repayments"
            let p = monthlyParameters 1100_00uL<Cent> 12u<OffsetDay> 6u
            let actual = calculateBasicSchedule p
            actual |> BasicSchedule.outputHtmlToFile folder title description p

            let expected = {
                EvaluationDay = 0u<OffsetDay>
                Items = actual.Items
                Stats = {
                    InitialInterestBalance = 0uL<Cent>
                    LastScheduledPaymentDay = 164u<OffsetDay>
                    LevelPayment = 323_82uL<Cent>
                    FinalPayment = 323_79uL<Cent>
                    ScheduledPaymentTotal = 1942_89uL<Cent>
                    PrincipalTotal = 1100_00uL<Cent>
                    InterestTotal = 842_89uL<Cent>
                    InitialApr = Percent 1296.2m
                    InitialCostToBorrowingRatio = Percent 76.63m
                }
            }

            actual |> should equal expected


        [<Fact>]
        let PaymentScheduleTest_Monthly_1100_fp16_r6 () =
            let title = "PaymentScheduleTest_Monthly_1100_fp16_r6"
            let description = "£1100 with 16 days to first payment and 6 repayments"
            let p = monthlyParameters 1100_00uL<Cent> 16u<OffsetDay> 6u
            let actual = calculateBasicSchedule p
            actual |> BasicSchedule.outputHtmlToFile folder title description p

            let expected = {
                EvaluationDay = 0u<OffsetDay>
                Items = actual.Items
                Stats = {
                    InitialInterestBalance = 0uL<Cent>
                    LastScheduledPaymentDay = 168u<OffsetDay>
                    LevelPayment = 333_25uL<Cent>
                    FinalPayment = 333_24uL<Cent>
                    ScheduledPaymentTotal = 1999_49uL<Cent>
                    PrincipalTotal = 1100_00uL<Cent>
                    InterestTotal = 899_49uL<Cent>
                    InitialApr = Percent 1294.9m
                    InitialCostToBorrowingRatio = Percent 81.77m
                }
            }

            actual |> should equal expected


        [<Fact>]
        let PaymentScheduleTest_Monthly_1100_fp20_r5 () =
            let title = "PaymentScheduleTest_Monthly_1100_fp20_r5"
            let description = "£1100 with 20 days to first payment and 5 repayments"
            let p = monthlyParameters 1100_00uL<Cent> 20u<OffsetDay> 5u
            let actual = calculateBasicSchedule p
            actual |> BasicSchedule.outputHtmlToFile folder title description p

            let expected = {
                EvaluationDay = 0u<OffsetDay>
                Items = actual.Items
                Stats = {
                    InitialInterestBalance = 0uL<Cent>
                    LastScheduledPaymentDay = 142u<OffsetDay>
                    LevelPayment = 376_87uL<Cent>
                    FinalPayment = 376_82uL<Cent>
                    ScheduledPaymentTotal = 1884_30uL<Cent>
                    PrincipalTotal = 1100_00uL<Cent>
                    InterestTotal = 784_30uL<Cent>
                    InitialApr = Percent 1292.9m
                    InitialCostToBorrowingRatio = Percent 71.3m
                }
            }

            actual |> should equal expected


        [<Fact>]
        let PaymentScheduleTest_Monthly_1100_fp24_r5 () =
            let title = "PaymentScheduleTest_Monthly_1100_fp24_r5"
            let description = "£1100 with 24 days to first payment and 5 repayments"
            let p = monthlyParameters 1100_00uL<Cent> 24u<OffsetDay> 5u
            let actual = calculateBasicSchedule p
            actual |> BasicSchedule.outputHtmlToFile folder title description p

            let expected = {
                EvaluationDay = 0u<OffsetDay>
                Items = actual.Items
                Stats = {
                    InitialInterestBalance = 0uL<Cent>
                    LastScheduledPaymentDay = 145u<OffsetDay>
                    LevelPayment = 385_98uL<Cent>
                    FinalPayment = 385_97uL<Cent>
                    ScheduledPaymentTotal = 1929_89uL<Cent>
                    PrincipalTotal = 1100_00uL<Cent>
                    InterestTotal = 829_89uL<Cent>
                    InitialApr = Percent 1283.5m
                    InitialCostToBorrowingRatio = Percent 75.44m
                }
            }

            actual |> should equal expected


        [<Fact>]
        let PaymentScheduleTest_Monthly_1100_fp28_r5 () =
            let title = "PaymentScheduleTest_Monthly_1100_fp28_r5"
            let description = "£1100 with 28 days to first payment and 5 repayments"
            let p = monthlyParameters 1100_00uL<Cent> 28u<OffsetDay> 5u
            let actual = calculateBasicSchedule p
            actual |> BasicSchedule.outputHtmlToFile folder title description p

            let expected = {
                EvaluationDay = 0u<OffsetDay>
                Items = actual.Items
                Stats = {
                    InitialInterestBalance = 0uL<Cent>
                    LastScheduledPaymentDay = 149u<OffsetDay>
                    LevelPayment = 396_32uL<Cent>
                    FinalPayment = 396_30uL<Cent>
                    ScheduledPaymentTotal = 1981_58uL<Cent>
                    PrincipalTotal = 1100_00uL<Cent>
                    InterestTotal = 881_58uL<Cent>
                    InitialApr = Percent 1267.9m
                    InitialCostToBorrowingRatio = Percent 80.14m
                }
            }

            actual |> should equal expected


        [<Fact>]
        let PaymentScheduleTest_Monthly_1100_fp32_r5 () =
            let title = "PaymentScheduleTest_Monthly_1100_fp32_r5"
            let description = "£1100 with 32 days to first payment and 5 repayments"
            let p = monthlyParameters 1100_00uL<Cent> 32u<OffsetDay> 5u
            let actual = calculateBasicSchedule p
            actual |> BasicSchedule.outputHtmlToFile folder title description p

            let expected = {
                EvaluationDay = 0u<OffsetDay>
                Items = actual.Items
                Stats = {
                    InitialInterestBalance = 0uL<Cent>
                    LastScheduledPaymentDay = 153u<OffsetDay>
                    LevelPayment = 406_66uL<Cent>
                    FinalPayment = 406_66uL<Cent>
                    ScheduledPaymentTotal = 2033_30uL<Cent>
                    PrincipalTotal = 1100_00uL<Cent>
                    InterestTotal = 933_30uL<Cent>
                    InitialApr = Percent 1249.8m
                    InitialCostToBorrowingRatio = Percent 84.85m
                }
            }

            actual |> should equal expected


        [<Fact>]
        let PaymentScheduleTest_Monthly_1300_fp04_r6 () =
            let title = "PaymentScheduleTest_Monthly_1300_fp04_r6"
            let description = "£1300 with 04 days to first payment and 6 repayments"
            let p = monthlyParameters 1300_00uL<Cent> 4u<OffsetDay> 6u
            let actual = calculateBasicSchedule p
            actual |> BasicSchedule.outputHtmlToFile folder title description p

            let expected = {
                EvaluationDay = 0u<OffsetDay>
                Items = actual.Items
                Stats = {
                    InitialInterestBalance = 0uL<Cent>
                    LastScheduledPaymentDay = 156u<OffsetDay>
                    LevelPayment = 360_40uL<Cent>
                    FinalPayment = 360_37uL<Cent>
                    ScheduledPaymentTotal = 2162_37uL<Cent>
                    PrincipalTotal = 1300_00uL<Cent>
                    InterestTotal = 862_37uL<Cent>
                    InitialApr = Percent 1277.7m
                    InitialCostToBorrowingRatio = Percent 66.34m
                }
            }

            actual |> should equal expected


        [<Fact>]
        let PaymentScheduleTest_Monthly_1300_fp08_r6 () =
            let title = "PaymentScheduleTest_Monthly_1300_fp08_r6"
            let description = "£1300 with 08 days to first payment and 6 repayments"

            let p = {
                monthlyParameters 1300_00uL<Cent> 8u<OffsetDay> 6u with
                    PaymentConfig.LevelPaymentOption = SimilarFinalPayment
            }

            let actual = calculateBasicSchedule p
            actual |> BasicSchedule.outputHtmlToFile folder title description p

            let expected = {
                EvaluationDay = 0u<OffsetDay>
                Items = actual.Items
                Stats = {
                    InitialInterestBalance = 0uL<Cent>
                    LastScheduledPaymentDay = 160u<OffsetDay>
                    LevelPayment = 371_55uL<Cent>
                    FinalPayment = 371_49uL<Cent>
                    ScheduledPaymentTotal = 2229_24uL<Cent>
                    PrincipalTotal = 1300_00uL<Cent>
                    InterestTotal = 929_24uL<Cent>
                    InitialApr = Percent 1291m
                    InitialCostToBorrowingRatio = Percent 71.48m
                }
            }

            actual |> should equal expected


        [<Fact>]
        let PaymentScheduleTest_Monthly_1300_fp12_r6 () =
            let title = "PaymentScheduleTest_Monthly_1300_fp12_r6"
            let description = "£1300 with 12 days to first payment and 6 repayments"

            let p = {
                monthlyParameters 1300_00uL<Cent> 12u<OffsetDay> 6u with
                    PaymentConfig.LevelPaymentOption = SimilarFinalPayment
            }

            let actual = calculateBasicSchedule p
            actual |> BasicSchedule.outputHtmlToFile folder title description p

            let expected = {
                EvaluationDay = 0u<OffsetDay>
                Items = actual.Items
                Stats = {
                    InitialInterestBalance = 0uL<Cent>
                    LastScheduledPaymentDay = 164u<OffsetDay>
                    LevelPayment = 382_69uL<Cent>
                    FinalPayment = 382_74uL<Cent>
                    ScheduledPaymentTotal = 2296_19uL<Cent>
                    PrincipalTotal = 1300_00uL<Cent>
                    InterestTotal = 996_19uL<Cent>
                    InitialApr = Percent 1296.2m
                    InitialCostToBorrowingRatio = Percent 76.63m
                }
            }

            actual |> should equal expected


        [<Fact>]
        let PaymentScheduleTest_Monthly_1300_fp16_r6 () =
            let title = "PaymentScheduleTest_Monthly_1300_fp16_r6"
            let description = "£1300 with 16 days to first payment and 6 repayments"

            let p = {
                monthlyParameters 1300_00uL<Cent> 16u<OffsetDay> 6u with
                    PaymentConfig.LevelPaymentOption = SimilarFinalPayment
            }

            let actual = calculateBasicSchedule p
            actual |> BasicSchedule.outputHtmlToFile folder title description p

            let expected = {
                EvaluationDay = 0u<OffsetDay>
                Items = actual.Items
                Stats = {
                    InitialInterestBalance = 0uL<Cent>
                    LastScheduledPaymentDay = 168u<OffsetDay>
                    LevelPayment = 393_84uL<Cent>
                    FinalPayment = 393_86uL<Cent>
                    ScheduledPaymentTotal = 2363_06uL<Cent>
                    PrincipalTotal = 1300_00uL<Cent>
                    InterestTotal = 1063_06uL<Cent>
                    InitialApr = Percent 1295m
                    InitialCostToBorrowingRatio = Percent 81.77m
                }
            }

            actual |> should equal expected


        [<Fact>]
        let PaymentScheduleTest_Monthly_1300_fp20_r6 () =
            let title = "PaymentScheduleTest_Monthly_1300_fp20_r6"
            let description = "£1300 with 20 days to first payment and 6 repayments"
            let p = monthlyParameters 1300_00uL<Cent> 20u<OffsetDay> 6u
            let actual = calculateBasicSchedule p
            actual |> BasicSchedule.outputHtmlToFile folder title description p

            let expected = {
                EvaluationDay = 0u<OffsetDay>
                Items = actual.Items
                Stats = {
                    InitialInterestBalance = 0uL<Cent>
                    LastScheduledPaymentDay = 172u<OffsetDay>
                    LevelPayment = 404_99uL<Cent>
                    FinalPayment = 404_97uL<Cent>
                    ScheduledPaymentTotal = 2429_92uL<Cent>
                    PrincipalTotal = 1300_00uL<Cent>
                    InterestTotal = 1129_92uL<Cent>
                    InitialApr = Percent 1288.6m
                    InitialCostToBorrowingRatio = Percent 86.92m
                }
            }

            actual |> should equal expected


        [<Fact>]
        let PaymentScheduleTest_Monthly_1300_fp24_r6 () =
            let title = "PaymentScheduleTest_Monthly_1300_fp24_r6"
            let description = "£1300 with 24 days to first payment and 6 repayments"
            let p = monthlyParameters 1300_00uL<Cent> 24u<OffsetDay> 6u
            let actual = calculateBasicSchedule p
            actual |> BasicSchedule.outputHtmlToFile folder title description p

            let expected = {
                EvaluationDay = 0u<OffsetDay>
                Items = actual.Items
                Stats = {
                    InitialInterestBalance = 0uL<Cent>
                    LastScheduledPaymentDay = 176u<OffsetDay>
                    LevelPayment = 414_91uL<Cent>
                    FinalPayment = 414_90uL<Cent>
                    ScheduledPaymentTotal = 2489_45uL<Cent>
                    PrincipalTotal = 1300_00uL<Cent>
                    InterestTotal = 1189_45uL<Cent>
                    InitialApr = Percent 1280.4m
                    InitialCostToBorrowingRatio = Percent 91.5m
                }
            }

            actual |> should equal expected


        [<Fact>]
        let PaymentScheduleTest_Monthly_1300_fp28_r6 () =
            let title = "PaymentScheduleTest_Monthly_1300_fp28_r6"
            let description = "£1300 with 28 days to first payment and 6 repayments"

            let p = {
                monthlyParameters 1300_00uL<Cent> 28u<OffsetDay> 6u with
                    PaymentConfig.LevelPaymentOption = SimilarFinalPayment
            }

            let actual = calculateBasicSchedule p
            actual |> BasicSchedule.outputHtmlToFile folder title description p

            let expected = {
                EvaluationDay = 0u<OffsetDay>
                Items = actual.Items
                Stats = {
                    InitialInterestBalance = 0uL<Cent>
                    LastScheduledPaymentDay = 180u<OffsetDay>
                    LevelPayment = 426_02uL<Cent>
                    FinalPayment = 426_04uL<Cent>
                    ScheduledPaymentTotal = 2556_14uL<Cent>
                    PrincipalTotal = 1300_00uL<Cent>
                    InterestTotal = 1256_14uL<Cent>
                    InitialApr = Percent 1266.6m
                    InitialCostToBorrowingRatio = Percent 96.63m
                }
            }

            actual |> should equal expected


        [<Fact>]
        let PaymentScheduleTest_Monthly_1300_fp32_r5 () =
            let title = "PaymentScheduleTest_Monthly_1300_fp32_r5"
            let description = "£1300 with 32 days to first payment and 5 repayments"
            let p = monthlyParameters 1300_00uL<Cent> 32u<OffsetDay> 5u
            let actual = calculateBasicSchedule p
            actual |> BasicSchedule.outputHtmlToFile folder title description p

            let expected = {
                EvaluationDay = 0u<OffsetDay>
                Items = actual.Items
                Stats = {
                    InitialInterestBalance = 0uL<Cent>
                    LastScheduledPaymentDay = 153u<OffsetDay>
                    LevelPayment = 480_60uL<Cent>
                    FinalPayment = 480_58uL<Cent>
                    ScheduledPaymentTotal = 2402_98uL<Cent>
                    PrincipalTotal = 1300_00uL<Cent>
                    InterestTotal = 1102_98uL<Cent>
                    InitialApr = Percent 1249.8m
                    InitialCostToBorrowingRatio = Percent 84.84m
                }
            }

            actual |> should equal expected


        [<Fact>]
        let PaymentScheduleTest_Monthly_1500_fp04_r6 () =
            let title = "PaymentScheduleTest_Monthly_1500_fp04_r6"
            let description = "£1500 with 04 days to first payment and 6 repayments"

            let p = {
                monthlyParameters 1500_00uL<Cent> 4u<OffsetDay> 6u with
                    PaymentConfig.LevelPaymentOption = SimilarFinalPayment
            }

            let actual = calculateBasicSchedule p
            actual |> BasicSchedule.outputHtmlToFile folder title description p

            let expected = {
                EvaluationDay = 0u<OffsetDay>
                Items = actual.Items
                Stats = {
                    InitialInterestBalance = 0uL<Cent>
                    LastScheduledPaymentDay = 156u<OffsetDay>
                    LevelPayment = 415_84uL<Cent>
                    FinalPayment = 415_86uL<Cent>
                    ScheduledPaymentTotal = 2495_06uL<Cent>
                    PrincipalTotal = 1500_00uL<Cent>
                    InterestTotal = 995_06uL<Cent>
                    InitialApr = Percent 1277.7m
                    InitialCostToBorrowingRatio = Percent 66.34m
                }
            }

            actual |> should equal expected


        [<Fact>]
        let PaymentScheduleTest_Monthly_1500_fp08_r6 () =
            let title = "PaymentScheduleTest_Monthly_1500_fp08_r6"
            let description = "£1500 with 08 days to first payment and 6 repayments"
            let p = monthlyParameters 1500_00uL<Cent> 8u<OffsetDay> 6u
            let actual = calculateBasicSchedule p
            actual |> BasicSchedule.outputHtmlToFile folder title description p

            let expected = {
                EvaluationDay = 0u<OffsetDay>
                Items = actual.Items
                Stats = {
                    InitialInterestBalance = 0uL<Cent>
                    LastScheduledPaymentDay = 160u<OffsetDay>
                    LevelPayment = 428_71uL<Cent>
                    FinalPayment = 428_65uL<Cent>
                    ScheduledPaymentTotal = 2572_20uL<Cent>
                    PrincipalTotal = 1500_00uL<Cent>
                    InterestTotal = 1072_20uL<Cent>
                    InitialApr = Percent 1290.9m
                    InitialCostToBorrowingRatio = Percent 71.48m
                }
            }

            actual |> should equal expected


        [<Fact>]
        let PaymentScheduleTest_Monthly_1500_fp12_r6 () =
            let title = "PaymentScheduleTest_Monthly_1500_fp12_r6"
            let description = "£1500 with 12 days to first payment and 6 repayments"
            let p = monthlyParameters 1500_00uL<Cent> 12u<OffsetDay> 6u
            let actual = calculateBasicSchedule p
            actual |> BasicSchedule.outputHtmlToFile folder title description p

            let expected = {
                EvaluationDay = 0u<OffsetDay>
                Items = actual.Items
                Stats = {
                    InitialInterestBalance = 0uL<Cent>
                    LastScheduledPaymentDay = 164u<OffsetDay>
                    LevelPayment = 441_57uL<Cent>
                    FinalPayment = 441_57uL<Cent>
                    ScheduledPaymentTotal = 2649_42uL<Cent>
                    PrincipalTotal = 1500_00uL<Cent>
                    InterestTotal = 1149_42uL<Cent>
                    InitialApr = Percent 1296.2m
                    InitialCostToBorrowingRatio = Percent 76.63m
                }
            }

            actual |> should equal expected


        [<Fact>]
        let PaymentScheduleTest_Monthly_1500_fp16_r6 () =
            let title = "PaymentScheduleTest_Monthly_1500_fp16_r6"
            let description = "£1500 with 16 days to first payment and 6 repayments"

            let p = {
                monthlyParameters 1500_00uL<Cent> 16u<OffsetDay> 6u with
                    PaymentConfig.LevelPaymentOption = SimilarFinalPayment
            }

            let actual = calculateBasicSchedule p
            actual |> BasicSchedule.outputHtmlToFile folder title description p

            let expected = {
                EvaluationDay = 0u<OffsetDay>
                Items = actual.Items
                Stats = {
                    InitialInterestBalance = 0uL<Cent>
                    LastScheduledPaymentDay = 168u<OffsetDay>
                    LevelPayment = 454_43uL<Cent>
                    FinalPayment = 454_45uL<Cent>
                    ScheduledPaymentTotal = 2726_60uL<Cent>
                    PrincipalTotal = 1500_00uL<Cent>
                    InterestTotal = 1226_60uL<Cent>
                    InitialApr = Percent 1295m
                    InitialCostToBorrowingRatio = Percent 81.77m
                }
            }

            actual |> should equal expected


        [<Fact>]
        let PaymentScheduleTest_Monthly_1500_fp20_r6 () =
            let title = "PaymentScheduleTest_Monthly_1500_fp20_r6"
            let description = "£1500 with 20 days to first payment and 6 repayments"

            let p = {
                monthlyParameters 1500_00uL<Cent> 20u<OffsetDay> 6u with
                    PaymentConfig.LevelPaymentOption = HigherFinalPayment
            }

            let actual = calculateBasicSchedule p
            actual |> BasicSchedule.outputHtmlToFile folder title description p

            let expected = {
                EvaluationDay = 0u<OffsetDay>
                Items = actual.Items
                Stats = {
                    InitialInterestBalance = 0uL<Cent>
                    LastScheduledPaymentDay = 172u<OffsetDay>
                    LevelPayment = 467_29uL<Cent>
                    FinalPayment = 467_33uL<Cent>
                    ScheduledPaymentTotal = 2803_78uL<Cent>
                    PrincipalTotal = 1500_00uL<Cent>
                    InterestTotal = 1303_78uL<Cent>
                    InitialApr = Percent 1288.6m
                    InitialCostToBorrowingRatio = Percent 86.92m
                }
            }

            actual |> should equal expected


        [<Fact>]
        let PaymentScheduleTest_Monthly_1500_fp24_r6 () =
            let title = "PaymentScheduleTest_Monthly_1500_fp24_r6"
            let description = "£1500 with 24 days to first payment and 6 repayments"

            let p = {
                monthlyParameters 1500_00uL<Cent> 24u<OffsetDay> 6u with
                    PaymentConfig.LevelPaymentOption = SimilarFinalPayment
            }

            let actual = calculateBasicSchedule p
            actual |> BasicSchedule.outputHtmlToFile folder title description p

            let expected = {
                EvaluationDay = 0u<OffsetDay>
                Items = actual.Items
                Stats = {
                    InitialInterestBalance = 0uL<Cent>
                    LastScheduledPaymentDay = 176u<OffsetDay>
                    LevelPayment = 478_74uL<Cent>
                    FinalPayment = 478_76uL<Cent>
                    ScheduledPaymentTotal = 2872_46uL<Cent>
                    PrincipalTotal = 1500_00uL<Cent>
                    InterestTotal = 1372_46uL<Cent>
                    InitialApr = Percent 1280.4m
                    InitialCostToBorrowingRatio = Percent 91.5m
                }
            }

            actual |> should equal expected


        [<Fact>]
        let PaymentScheduleTest_Monthly_1500_fp28_r6 () =
            let title = "PaymentScheduleTest_Monthly_1500_fp28_r6"
            let description = "£1500 with 28 days to first payment and 6 repayments"
            let p = monthlyParameters 1500_00uL<Cent> 28u<OffsetDay> 6u
            let actual = calculateBasicSchedule p
            actual |> BasicSchedule.outputHtmlToFile folder title description p

            let expected = {
                EvaluationDay = 0u<OffsetDay>
                Items = actual.Items
                Stats = {
                    InitialInterestBalance = 0uL<Cent>
                    LastScheduledPaymentDay = 180u<OffsetDay>
                    LevelPayment = 491_57uL<Cent>
                    FinalPayment = 491_52uL<Cent>
                    ScheduledPaymentTotal = 2949_37uL<Cent>
                    PrincipalTotal = 1500_00uL<Cent>
                    InterestTotal = 1449_37uL<Cent>
                    InitialApr = Percent 1266.6m
                    InitialCostToBorrowingRatio = Percent 96.62m
                }
            }

            actual |> should equal expected


        [<Fact>]
        let PaymentScheduleTest_Monthly_1500_fp32_r5 () =
            let title = "PaymentScheduleTest_Monthly_1500_fp32_r5"
            let description = "£1500 with 32 days to first payment and 5 repayments"

            let p = {
                monthlyParameters 1500_00uL<Cent> 32u<OffsetDay> 5u with
                    PaymentConfig.LevelPaymentOption = SimilarFinalPayment
            }

            let actual = calculateBasicSchedule p
            actual |> BasicSchedule.outputHtmlToFile folder title description p

            let expected = {
                EvaluationDay = 0u<OffsetDay>
                Items = actual.Items
                Stats = {
                    InitialInterestBalance = 0uL<Cent>
                    LastScheduledPaymentDay = 153u<OffsetDay>
                    LevelPayment = 554_53uL<Cent>
                    FinalPayment = 554_57uL<Cent>
                    ScheduledPaymentTotal = 2772_69uL<Cent>
                    PrincipalTotal = 1500_00uL<Cent>
                    InterestTotal = 1272_69uL<Cent>
                    InitialApr = Percent 1249.8m
                    InitialCostToBorrowingRatio = Percent 84.85m
                }
            }

            actual |> should equal expected

    let parameters: BasicParameters = {
        EvaluationDate = Date(2022, 12, 19)
        StartDate = Date(2022, 12, 19)
        Principal = 300_00uL<Cent>
        ScheduleConfig =
            AutoGenerateSchedule {
                UnitPeriodConfig = Daily(Date(2023, 1, 3))
                ScheduleLength = PaymentCount 1u
            }
        PaymentConfig = {
            LevelPaymentOption = LowerFinalPayment
            Rounding = RoundUp
        }
        FeeConfig = ValueNone
        InterestConfig = {
            Method = Interest.Method.Actuarial
            StandardRate = Interest.Rate.Daily(Percent 0.8m)
            Cap = interestCapExample
            Rounding = RoundDown
            AprMethod = Apr.CalculationMethod.UnitedKingdom
            AprPrecision = 3u
        }
    }

    [<Fact>]
    let PaymentScheduleTest001 () =
        let title = "PaymentScheduleTest001"

        let description =
            "If there are no other payments, level payment should equal final payment"

        let actual =
            let schedule = calculateBasicSchedule parameters
            schedule |> BasicSchedule.outputHtmlToFile folder title description parameters
            schedule.Stats.LevelPayment, schedule.Stats.FinalPayment

        let expected = 336_00uL<Cent>, 336_00L<Cent>

        actual |> should equal expected

    [<Fact>]
    let PaymentScheduleTest002 () =
        let title = "PaymentScheduleTest002"
        let description = "Term must not exceed maximum duration"

        let startDate = Date(2024, 5, 8)

        let p = {
            parameters with
                EvaluationDate = Date(2024, 5, 8)
                StartDate = startDate
                Principal = 1000_00uL<Cent>
                ScheduleConfig =
                    AutoGenerateSchedule {
                        UnitPeriodConfig = Monthly(1u, 2024, 5, 8)
                        ScheduleLength = MaxDuration(startDate, 183u<OffsetDay>)
                    }
        }

        let actual =
            let schedule = calculateBasicSchedule p
            schedule |> BasicSchedule.outputHtmlToFile folder title description p
            schedule.Stats.LevelPayment, schedule.Stats.FinalPayment

        let expected = 269_20uL<Cent>, 269_11L<Cent>

        actual |> should equal expected

    [<Fact>]
    let PaymentScheduleTest003 () =
        let title = "PaymentScheduleTest003"
        let description = "Term must not exceed maximum duration"

        let startDate = Date(2024, 5, 8)

        let p = {
            parameters with
                EvaluationDate = Date(2024, 5, 8)
                StartDate = startDate
                Principal = 1000_00uL<Cent>
                ScheduleConfig =
                    AutoGenerateSchedule {
                        UnitPeriodConfig = Monthly(1u, 2024, 5, 18)
                        ScheduleLength = MaxDuration(startDate, 184u<OffsetDay>)
                    }
        }

        let actual =
            let schedule = calculateBasicSchedule p
            schedule |> BasicSchedule.outputHtmlToFile folder title description p
            schedule.Stats.LevelPayment, schedule.Stats.FinalPayment

        let expected = 290_73uL<Cent>, 290_71L<Cent>

        actual |> should equal expected

    [<Fact>]
    let PaymentScheduleTest004 () =
        let title = "PaymentScheduleTest004"
        let description = "Payment count must not be exceeded"

        let p = {
            parameters with
                EvaluationDate = Date(2024, 6, 24)
                StartDate = Date(2024, 6, 24)
                Principal = 100_00uL<Cent>
                ScheduleConfig =
                    AutoGenerateSchedule {
                        UnitPeriodConfig = Monthly(1u, 2024, 7, 4)
                        ScheduleLength = PaymentCount 4u
                    }
                PaymentConfig.Rounding = RoundWith MidpointRounding.ToEven
                InterestConfig.Rounding = RoundWith MidpointRounding.ToEven
        }

        let actual =
            let schedule = calculateBasicSchedule p
            schedule |> BasicSchedule.outputHtmlToFile folder title description p
            schedule.Stats.LevelPayment, schedule.Stats.FinalPayment

        let expected = 36_48uL<Cent>, 36_44L<Cent>

        actual |> should equal expected

    [<Fact>]
    let PaymentScheduleTest005 () =
        let title = "PaymentScheduleTest005"

        let description =
            "Calculation with three equivalent but different payment schedules types should be identical"

        let p paymentSchedule = {
            parameters with
                EvaluationDate = Date(2024, 6, 24)
                StartDate = Date(2024, 6, 24)
                Principal = 100_00uL<Cent>
                ScheduleConfig = paymentSchedule
                PaymentConfig.Rounding = RoundWith MidpointRounding.ToEven
                InterestConfig.Rounding = RoundWith MidpointRounding.ToEven
        }

        let actual =
            let paymentSchedule1 =
                AutoGenerateSchedule {
                    UnitPeriodConfig = Monthly(1u, 2024, 7, 4)
                    ScheduleLength = PaymentCount 4u
                }

            let paymentSchedule2 =
                FixedSchedules [|
                    {
                        UnitPeriodConfig = Monthly(1u, 2024, 7, 4)
                        PaymentCount = 3u
                        PaymentValue = 36_48uL<Cent>
                        ScheduleType = ScheduleType.Original
                    }
                    {
                        UnitPeriodConfig = Monthly(1u, 2024, 10, 4)
                        PaymentCount = 1u
                        PaymentValue = 36_44uL<Cent>
                        ScheduleType = ScheduleType.Original
                    }
                |]

            let paymentSchedule3 =
                CustomSchedule
                <| Map [
                    10u<OffsetDay>, ScheduledPayment.quick (ValueSome 36_48uL<Cent>) ValueNone
                    41u<OffsetDay>, ScheduledPayment.quick (ValueSome 36_48uL<Cent>) ValueNone
                    72u<OffsetDay>, ScheduledPayment.quick (ValueSome 36_48uL<Cent>) ValueNone
                    102u<OffsetDay>, ScheduledPayment.quick (ValueSome 36_44uL<Cent>) ValueNone
                ]

            let schedule1 = p paymentSchedule1 |> fun bp -> calculateBasicSchedule bp
            let schedule2 = p paymentSchedule2 |> fun bp -> calculateBasicSchedule bp
            let schedule3 = p paymentSchedule3 |> fun bp -> calculateBasicSchedule bp

            let html1 = schedule1 |> BasicSchedule.toHtmlTable
            let html2 = schedule2 |> BasicSchedule.toHtmlTable
            let html3 = schedule3 |> BasicSchedule.toHtmlTable

            $"{title}<br />{description}<br />{html1}<br />{html2}<br />{html3}"
            |> outputToFile' $"out/{folder}/{title}.md" false

            schedule1 = schedule2 && schedule2 = schedule3

        actual |> should equal true
