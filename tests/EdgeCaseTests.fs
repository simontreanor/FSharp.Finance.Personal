namespace FSharp.Finance.Personal.Tests

open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal

module EdgeCaseTests =

    let folder = "EdgeCase"

    open Amortisation
    open AppliedPayment
    open Calculation
    open DateDay
    open Scheduling
    open Quotes
    open Refinancing
    open UnitPeriod

    let parameters1: Parameters = {
        Basic = {
            EvaluationDate = Date(2024, 3, 12)
            StartDate = Date(2023, 2, 9)
            Principal = 30000L<Cent>
            ScheduleConfig =
                CustomSchedule
                <| Map [
                    15u<OffsetDay>, ScheduledPayment.quick (ValueSome 137_40L<Cent>) ValueNone
                    43u<OffsetDay>, ScheduledPayment.quick (ValueSome 137_40L<Cent>) ValueNone
                    74u<OffsetDay>, ScheduledPayment.quick (ValueSome 137_40L<Cent>) ValueNone
                    104u<OffsetDay>, ScheduledPayment.quick (ValueSome 137_40L<Cent>) ValueNone
                ]
            PaymentConfig = {
                LevelPaymentOption = LowerFinalPayment
                Rounding = RoundUp
            }
            FeeConfig =
                ValueSome {
                    FeeType = Fee.FeeType.CabOrCsoFee(Amount.Percentage(Percent 154.47m, Restriction.NoLimit))
                    Rounding = RoundDown
                    FeeAmortisation = Fee.FeeAmortisation.AmortiseProportionately
                }
            InterestConfig = {
                Method = Interest.Method.Actuarial
                StandardRate = Interest.Rate.Annual <| Percent 9.95m
                Cap = Interest.Cap.zero
                AprMethod = Apr.CalculationMethod.UsActuarial
                AprPrecision = 5u
                Rounding = RoundDown
            }
        }
        Advanced = {
            PaymentConfig = {
                ScheduledPaymentOption = AsScheduled
                Minimum = DeferOrWriteOff 50L<Cent>
                Timeout = 3u<OffsetDay>
            }
            FeeConfig =
                ValueSome {
                    SettlementRebate = Fee.SettlementRebate.ProRata
                }
            ChargeConfig = None
            InterestConfig = {
                InitialGracePeriod = 3u<OffsetDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = Interest.Rate.Zero
            }
            SettlementDay = SettlementDay.NoSettlement
            TrimEnd = false
        }
    }

    [<Fact>]
    let EdgeCaseTest000 () =
        let title = "EdgeCaseTest000"
        let description = "Quote returning nothing"

        let actualPayments =
            Map [ 5u<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 31200L<Cent> ] ]

        let actual =
            let quote = getQuote parameters1 actualPayments

            quote.Schedules
            |> Schedule.outputHtmlToFile folder title description parameters1 ""

            quote.QuoteResult

        let expected =
            PaymentQuote {
                PaymentValue = 500_79L<Cent>
                Apportionment = {
                    PrincipalPortion = 177_81L<Cent>
                    FeePortion = 274_64L<Cent>
                    InterestPortion = 48_34L<Cent>
                    ChargesPortion = 0L<Cent>
                }
                FeeRebateIfSettled = 0L<Cent>
            }

        actual |> should equal expected

    [<Fact>]
    let EdgeCaseTest001 () =
        let title = "EdgeCaseTest001"
        let description = "Quote returning nothing"

        let p = {
            parameters1 with
                Basic.StartDate = Date(2022, 2, 2)
                Basic.Principal = 25000L<Cent>
                Basic.ScheduleConfig =
                    CustomSchedule
                    <| Map [
                        16u<OffsetDay>, ScheduledPayment.quick (ValueSome 11500L<Cent>) ValueNone
                        44u<OffsetDay>, ScheduledPayment.quick (ValueSome 11500L<Cent>) ValueNone
                        75u<OffsetDay>, ScheduledPayment.quick (ValueSome 11500L<Cent>) ValueNone
                        105u<OffsetDay>, ScheduledPayment.quick (ValueSome 11500L<Cent>) ValueNone
                    ]
        }

        let actualPayments =
            Map [ 5u<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 26000L<Cent> ] ]

        let actual =
            let quote = getQuote p actualPayments

            quote.Schedules |> Schedule.outputHtmlToFile folder title description p ""

            quote.QuoteResult

        let expected =
            PaymentQuote {
                PaymentValue = 455_55L<Cent>
                Apportionment = {
                    PrincipalPortion = 148_17L<Cent>
                    FeePortion = 228_86L<Cent>
                    InterestPortion = 78_52L<Cent>
                    ChargesPortion = 0L<Cent>
                }
                FeeRebateIfSettled = 0L<Cent>
            }

        actual |> should equal expected

    [<Fact>]
    let EdgeCaseTest002 () =
        let title = "EdgeCaseTest002"
        let description = "Quote returning nothing"

        let p = {
            parameters1 with
                Basic.StartDate = Date(2022, 12, 2)
                Basic.Principal = 75000L<Cent>
                Basic.ScheduleConfig =
                    CustomSchedule
                    <| Map [
                        14u<OffsetDay>, ScheduledPayment.quick (ValueSome 34350L<Cent>) ValueNone
                        45u<OffsetDay>, ScheduledPayment.quick (ValueSome 34350L<Cent>) ValueNone
                        76u<OffsetDay>, ScheduledPayment.quick (ValueSome 34350L<Cent>) ValueNone
                        104u<OffsetDay>, ScheduledPayment.quick (ValueSome 34350L<Cent>) ValueNone
                    ]
        }

        let actualPayments =
            Map [ 13u<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 82800L<Cent> ] ]

        let actual =
            let quote = getQuote p actualPayments

            quote.Schedules |> Schedule.outputHtmlToFile folder title description p ""

            quote.QuoteResult

        let expected =
            PaymentQuote {
                PaymentValue = 1221_54L<Cent>
                Apportionment = {
                    PrincipalPortion = 427_28L<Cent>
                    FeePortion = 660_00L<Cent>
                    InterestPortion = 134_26L<Cent>
                    ChargesPortion = 0L<Cent>
                }
                FeeRebateIfSettled = 0L<Cent>
            }

        actual |> should equal expected

    [<Fact>]
    let EdgeCaseTest003 () =
        let title = "EdgeCaseTest003"
        let description = "Quote returning nothing"

        let p = {
            parameters1 with
                Basic.StartDate = Date(2020, 10, 8)
                Basic.Principal = 50000L<Cent>
                Basic.ScheduleConfig =
                    CustomSchedule
                    <| Map [
                        8u<OffsetDay>, ScheduledPayment.quick (ValueSome 22500L<Cent>) ValueNone
                        39u<OffsetDay>, ScheduledPayment.quick (ValueSome 22500L<Cent>) ValueNone
                        69u<OffsetDay>, ScheduledPayment.quick (ValueSome 22500L<Cent>) ValueNone
                        100u<OffsetDay>, ScheduledPayment.quick (ValueSome 22500L<Cent>) ValueNone
                        214u<OffsetDay>, ScheduledPayment.quick (ValueSome 25000L<Cent>) ValueNone
                        245u<OffsetDay>, ScheduledPayment.quick (ValueSome 27600L<Cent>) ValueNone
                    ]
        }

        let actualPayments =
            Map.merge [|
                8u<OffsetDay>, [| 0, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                8u<OffsetDay>, [| 1, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                8u<OffsetDay>, [| 2, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                11u<OffsetDay>, [| 0, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                11u<OffsetDay>, [| 1, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                11u<OffsetDay>, [| 2, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                14u<OffsetDay>, [| 0, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                14u<OffsetDay>, [| 1, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                14u<OffsetDay>, [| 2, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                39u<OffsetDay>, [| 0, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                39u<OffsetDay>, [| 1, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                39u<OffsetDay>, [| 2, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                42u<OffsetDay>, [| 0, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                42u<OffsetDay>, [| 1, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                42u<OffsetDay>, [| 2, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                45u<OffsetDay>, [| 0, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                45u<OffsetDay>, [| 1, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                45u<OffsetDay>, [| 2, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                69u<OffsetDay>, [| 0, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                69u<OffsetDay>, [| 1, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                69u<OffsetDay>, [| 2, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                72u<OffsetDay>, [| 0, ActualPayment.quickConfirmed 22500L<Cent> |]
                72u<OffsetDay>, [| 1, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                72u<OffsetDay>, [| 2, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                75u<OffsetDay>, [| 0, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                75u<OffsetDay>, [| 1, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                75u<OffsetDay>, [| 2, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                100u<OffsetDay>, [| 0, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                100u<OffsetDay>, [| 1, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                100u<OffsetDay>, [| 2, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                103u<OffsetDay>, [| 0, ActualPayment.quickFailed 23700L<Cent> ValueNone |]
                103u<OffsetDay>, [| 1, ActualPayment.quickFailed 23700L<Cent> ValueNone |]
                103u<OffsetDay>, [| 2, ActualPayment.quickFailed 23700L<Cent> ValueNone |]
                106u<OffsetDay>, [| 0, ActualPayment.quickConfirmed 24900L<Cent> |]
                106u<OffsetDay>, [| 1, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                106u<OffsetDay>, [| 2, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                214u<OffsetDay>, [| 0, ActualPayment.quickFailed 25000L<Cent> ValueNone |]
                214u<OffsetDay>, [| 1, ActualPayment.quickFailed 25000L<Cent> ValueNone |]
                214u<OffsetDay>, [| 2, ActualPayment.quickFailed 25000L<Cent> ValueNone |]
                217u<OffsetDay>, [| 0, ActualPayment.quickFailed 25000L<Cent> ValueNone |]
                217u<OffsetDay>, [| 1, ActualPayment.quickFailed 25000L<Cent> ValueNone |]
                217u<OffsetDay>, [| 2, ActualPayment.quickFailed 25000L<Cent> ValueNone |]
                220u<OffsetDay>, [| 0, ActualPayment.quickFailed 25000L<Cent> ValueNone |]
                220u<OffsetDay>, [| 1, ActualPayment.quickFailed 25000L<Cent> ValueNone |]
                220u<OffsetDay>, [| 2, ActualPayment.quickFailed 25000L<Cent> ValueNone |]
                245u<OffsetDay>, [| 0, ActualPayment.quickFailed 25000L<Cent> ValueNone |]
                245u<OffsetDay>, [| 1, ActualPayment.quickFailed 25000L<Cent> ValueNone |]
                245u<OffsetDay>, [| 2, ActualPayment.quickFailed 25000L<Cent> ValueNone |]
                245u<OffsetDay>, [| 3, ActualPayment.quickFailed 25000L<Cent> ValueNone |]
                248u<OffsetDay>, [| 0, ActualPayment.quickFailed 25000L<Cent> ValueNone |]
                248u<OffsetDay>, [| 1, ActualPayment.quickFailed 25000L<Cent> ValueNone |]
                248u<OffsetDay>, [| 2, ActualPayment.quickFailed 25000L<Cent> ValueNone |]
                251u<OffsetDay>, [| 0, ActualPayment.quickFailed 25000L<Cent> ValueNone |]
                251u<OffsetDay>, [| 1, ActualPayment.quickFailed 25000L<Cent> ValueNone |]
                251u<OffsetDay>, [| 2, ActualPayment.quickFailed 25000L<Cent> ValueNone |]
                379u<OffsetDay>, [| 0, ActualPayment.quickFailed 17500L<Cent> ValueNone |]
                379u<OffsetDay>, [| 1, ActualPayment.quickFailed 17500L<Cent> ValueNone |]
                379u<OffsetDay>, [| 2, ActualPayment.quickFailed 17500L<Cent> ValueNone |]
                380u<OffsetDay>, [| 0, ActualPayment.quickConfirmed 17500L<Cent> |]
                407u<OffsetDay>, [| 0, ActualPayment.quickConfirmed 17500L<Cent> |]
                435u<OffsetDay>, [| 0, ActualPayment.quickFailed 17600L<Cent> ValueNone |]
                435u<OffsetDay>, [| 1, ActualPayment.quickFailed 17600L<Cent> ValueNone |]
                435u<OffsetDay>, [| 2, ActualPayment.quickFailed 17600L<Cent> ValueNone |]
                435u<OffsetDay>, [| 3, ActualPayment.quickFailed 17600L<Cent> ValueNone |]
                438u<OffsetDay>, [| 0, ActualPayment.quickFailed 17600L<Cent> ValueNone |]
                438u<OffsetDay>, [| 1, ActualPayment.quickFailed 17600L<Cent> ValueNone |]
                438u<OffsetDay>, [| 2, ActualPayment.quickFailed 17600L<Cent> ValueNone |]
                441u<OffsetDay>, [| 0, ActualPayment.quickFailed 17600L<Cent> ValueNone |]
                441u<OffsetDay>, [| 1, ActualPayment.quickFailed 17600L<Cent> ValueNone |]
                441u<OffsetDay>, [| 2, ActualPayment.quickFailed 17600L<Cent> ValueNone |]
                475u<OffsetDay>, [| 0, ActualPayment.quickConfirmed 17600L<Cent> |]
            |]

        let actual =
            let quote = getQuote p actualPayments

            quote.Schedules |> Schedule.outputHtmlToFile folder title description p ""

            quote.QuoteResult

        let expected =
            PaymentQuote {
                PaymentValue = 466_41L<Cent>
                Apportionment = {
                    PrincipalPortion = 151_32L<Cent>
                    FeePortion = 233_66L<Cent>
                    InterestPortion = 81_43L<Cent>
                    ChargesPortion = 0L<Cent>
                }
                FeeRebateIfSettled = 0L<Cent>
            }

        actual |> should equal expected

    [<Fact>]
    let ``EdgeCaseTest004`` () =
        let title = "EdgeCaseTest004"
        let description = "Only one insufficient funds charge per day"

        let p = {
            parameters1 with
                Basic.StartDate = Date(2020, 10, 8)
                Basic.Principal = 50000L<Cent>
                Basic.ScheduleConfig =
                    CustomSchedule
                    <| Map [
                        8u<OffsetDay>, ScheduledPayment.quick (ValueSome 22500L<Cent>) ValueNone
                        39u<OffsetDay>, ScheduledPayment.quick (ValueSome 22500L<Cent>) ValueNone
                        69u<OffsetDay>, ScheduledPayment.quick (ValueSome 22500L<Cent>) ValueNone
                        100u<OffsetDay>, ScheduledPayment.quick (ValueSome 22500L<Cent>) ValueNone
                        214u<OffsetDay>, ScheduledPayment.quick (ValueSome 25000L<Cent>) ValueNone
                        245u<OffsetDay>, ScheduledPayment.quick (ValueSome 27600L<Cent>) ValueNone
                    ]
                Advanced.ChargeConfig =
                    Some {
                        ChargeTypes =
                            Map [
                                Charge.InsufficientFunds,
                                {
                                    Value = 10_00L<Cent>
                                    ChargeGrouping = Charge.ChargeGrouping.OneChargeTypePerDay
                                    ChargeHolidays = [||]
                                }
                            ]
                    }
        }

        let actualPayments =
            Map.merge [|
                8u<OffsetDay>,
                [|
                    0, ActualPayment.quickFailed 22500L<Cent> (ValueSome Charge.InsufficientFunds)
                |]
                8u<OffsetDay>,
                [|
                    1, ActualPayment.quickFailed 22500L<Cent> (ValueSome Charge.InsufficientFunds)
                |]
                8u<OffsetDay>,
                [|
                    2, ActualPayment.quickFailed 22500L<Cent> (ValueSome Charge.InsufficientFunds)
                |]
                11u<OffsetDay>, [| 0, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                11u<OffsetDay>, [| 1, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                11u<OffsetDay>, [| 2, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                14u<OffsetDay>, [| 0, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                14u<OffsetDay>, [| 1, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                14u<OffsetDay>, [| 2, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                39u<OffsetDay>, [| 0, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                39u<OffsetDay>, [| 1, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                39u<OffsetDay>, [| 2, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                42u<OffsetDay>, [| 0, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                42u<OffsetDay>, [| 1, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                42u<OffsetDay>, [| 2, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                45u<OffsetDay>, [| 0, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                45u<OffsetDay>, [| 1, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                45u<OffsetDay>, [| 2, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                69u<OffsetDay>, [| 0, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                69u<OffsetDay>, [| 1, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                69u<OffsetDay>, [| 2, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                72u<OffsetDay>, [| 0, ActualPayment.quickConfirmed 22500L<Cent> |]
                72u<OffsetDay>, [| 1, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                72u<OffsetDay>, [| 2, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                75u<OffsetDay>, [| 0, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                75u<OffsetDay>, [| 1, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                75u<OffsetDay>, [| 2, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                100u<OffsetDay>, [| 0, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                100u<OffsetDay>, [| 1, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                100u<OffsetDay>, [| 2, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                103u<OffsetDay>, [| 0, ActualPayment.quickFailed 23700L<Cent> ValueNone |]
                103u<OffsetDay>, [| 1, ActualPayment.quickFailed 23700L<Cent> ValueNone |]
                103u<OffsetDay>, [| 2, ActualPayment.quickFailed 23700L<Cent> ValueNone |]
                106u<OffsetDay>, [| 0, ActualPayment.quickConfirmed 24900L<Cent> |]
                106u<OffsetDay>, [| 1, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                106u<OffsetDay>, [| 2, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                214u<OffsetDay>, [| 0, ActualPayment.quickFailed 25000L<Cent> ValueNone |]
                214u<OffsetDay>, [| 1, ActualPayment.quickFailed 25000L<Cent> ValueNone |]
                214u<OffsetDay>, [| 2, ActualPayment.quickFailed 25000L<Cent> ValueNone |]
                217u<OffsetDay>, [| 0, ActualPayment.quickFailed 25000L<Cent> ValueNone |]
                217u<OffsetDay>, [| 1, ActualPayment.quickFailed 25000L<Cent> ValueNone |]
                217u<OffsetDay>, [| 2, ActualPayment.quickFailed 25000L<Cent> ValueNone |]
                220u<OffsetDay>, [| 0, ActualPayment.quickFailed 25000L<Cent> ValueNone |]
                220u<OffsetDay>, [| 1, ActualPayment.quickFailed 25000L<Cent> ValueNone |]
                220u<OffsetDay>, [| 2, ActualPayment.quickFailed 25000L<Cent> ValueNone |]
                245u<OffsetDay>, [| 0, ActualPayment.quickFailed 25000L<Cent> ValueNone |]
                245u<OffsetDay>, [| 1, ActualPayment.quickFailed 25000L<Cent> ValueNone |]
                245u<OffsetDay>, [| 2, ActualPayment.quickFailed 25000L<Cent> ValueNone |]
                245u<OffsetDay>, [| 3, ActualPayment.quickFailed 25000L<Cent> ValueNone |]
                248u<OffsetDay>, [| 0, ActualPayment.quickFailed 25000L<Cent> ValueNone |]
                248u<OffsetDay>, [| 1, ActualPayment.quickFailed 25000L<Cent> ValueNone |]
                248u<OffsetDay>, [| 2, ActualPayment.quickFailed 25000L<Cent> ValueNone |]
                251u<OffsetDay>, [| 0, ActualPayment.quickFailed 25000L<Cent> ValueNone |]
                251u<OffsetDay>, [| 1, ActualPayment.quickFailed 25000L<Cent> ValueNone |]
                251u<OffsetDay>, [| 2, ActualPayment.quickFailed 25000L<Cent> ValueNone |]
                379u<OffsetDay>, [| 0, ActualPayment.quickFailed 17500L<Cent> ValueNone |]
                379u<OffsetDay>, [| 1, ActualPayment.quickFailed 17500L<Cent> ValueNone |]
                379u<OffsetDay>, [| 2, ActualPayment.quickFailed 17500L<Cent> ValueNone |]
                380u<OffsetDay>, [| 0, ActualPayment.quickConfirmed 17500L<Cent> |]
                407u<OffsetDay>, [| 0, ActualPayment.quickConfirmed 17500L<Cent> |]
                435u<OffsetDay>, [| 0, ActualPayment.quickFailed 17600L<Cent> ValueNone |]
                435u<OffsetDay>, [| 1, ActualPayment.quickFailed 17600L<Cent> ValueNone |]
                435u<OffsetDay>, [| 2, ActualPayment.quickFailed 17600L<Cent> ValueNone |]
                435u<OffsetDay>, [| 3, ActualPayment.quickFailed 17600L<Cent> ValueNone |]
                438u<OffsetDay>, [| 0, ActualPayment.quickFailed 17600L<Cent> ValueNone |]
                438u<OffsetDay>, [| 1, ActualPayment.quickFailed 17600L<Cent> ValueNone |]
                438u<OffsetDay>, [| 2, ActualPayment.quickFailed 17600L<Cent> ValueNone |]
                441u<OffsetDay>, [| 0, ActualPayment.quickFailed 17600L<Cent> ValueNone |]
                441u<OffsetDay>, [| 1, ActualPayment.quickFailed 17600L<Cent> ValueNone |]
                441u<OffsetDay>, [| 2, ActualPayment.quickFailed 17600L<Cent> ValueNone |]
                475u<OffsetDay>, [| 0, ActualPayment.quickConfirmed 17600L<Cent> |]
            |]

        let actual =
            let quote = getQuote p actualPayments

            quote.Schedules |> Schedule.outputHtmlToFile folder title description p ""

            quote.QuoteResult

        let expected =
            PaymentQuote {
                PaymentValue = 479_92L<Cent>
                Apportionment = {
                    PrincipalPortion = 155_70L<Cent>
                    FeePortion = 240_43L<Cent>
                    InterestPortion = 83_79L<Cent>
                    ChargesPortion = 0L<Cent>
                }
                FeeRebateIfSettled = 0L<Cent>
            }

        actual |> should equal expected

    let parameters2 = {
        parameters1 with
            Basic.StartDate = Date(2022, 6, 22)
            Basic.Principal = 500_00L<Cent>
            Basic.ScheduleConfig =
                AutoGenerateSchedule {
                    UnitPeriodConfig = Monthly(1, 2022, 7, 15)
                    ScheduleLength = PaymentCount 6
                }
            Basic.FeeConfig = ValueNone
            Basic.InterestConfig = {
                Method = Interest.Method.Actuarial
                StandardRate = Interest.Rate.Daily(Percent 0.8m)
                Cap = {
                    TotalAmount = Amount.Percentage(Percent 100m, Restriction.NoLimit)
                    DailyAmount = Amount.Percentage(Percent 0.8m, Restriction.NoLimit)
                }
                AprMethod = Apr.CalculationMethod.UnitedKingdom
                AprPrecision = 3u
                Rounding = RoundDown
            }
            Advanced.FeeConfig = ValueNone
            Advanced.InterestConfig = {
                parameters1.Advanced.InterestConfig with
                    InitialGracePeriod = 0u<OffsetDay>
            }
    }

    [<Fact>]
    let EdgeCaseTest005 () =
        let title = "EdgeCaseTest005"
        let description = "Quote returning nothing"

        let actualPayments =
            Map.merge [|
                23u<OffsetDay>, [| 0, ActualPayment.quickFailed 166_67L<Cent> ValueNone |]
                23u<OffsetDay>, [| 1, ActualPayment.quickFailed 66_67L<Cent> ValueNone |]
                23u<OffsetDay>, [| 2, ActualPayment.quickFailed 66_67L<Cent> ValueNone |]
                26u<OffsetDay>, [| 0, ActualPayment.quickFailed 66_67L<Cent> ValueNone |]
                26u<OffsetDay>, [| 1, ActualPayment.quickFailed 66_67L<Cent> ValueNone |]
                26u<OffsetDay>, [| 2, ActualPayment.quickFailed 66_67L<Cent> ValueNone |]
                29u<OffsetDay>, [| 0, ActualPayment.quickFailed 66_67L<Cent> ValueNone |]
                29u<OffsetDay>, [| 1, ActualPayment.quickFailed 66_67L<Cent> ValueNone |]
                29u<OffsetDay>, [| 2, ActualPayment.quickFailed 66_67L<Cent> ValueNone |]
                54u<OffsetDay>, [| 0, ActualPayment.quickFailed 66_67L<Cent> ValueNone |]
                54u<OffsetDay>, [| 1, ActualPayment.quickFailed 66_67L<Cent> ValueNone |]
                54u<OffsetDay>, [| 2, ActualPayment.quickFailed 66_67L<Cent> ValueNone |]
                57u<OffsetDay>, [| 0, ActualPayment.quickFailed 66_67L<Cent> ValueNone |]
                57u<OffsetDay>, [| 1, ActualPayment.quickFailed 66_67L<Cent> ValueNone |]
                57u<OffsetDay>, [| 2, ActualPayment.quickFailed 66_67L<Cent> ValueNone |]
                60u<OffsetDay>, [| 0, ActualPayment.quickFailed 66_67L<Cent> ValueNone |]
                60u<OffsetDay>, [| 1, ActualPayment.quickFailed 66_67L<Cent> ValueNone |]
                60u<OffsetDay>, [| 2, ActualPayment.quickFailed 66_67L<Cent> ValueNone |]
                85u<OffsetDay>, [| 0, ActualPayment.quickFailed 66_67L<Cent> ValueNone |]
                85u<OffsetDay>, [| 1, ActualPayment.quickFailed 66_67L<Cent> ValueNone |]
                85u<OffsetDay>, [| 2, ActualPayment.quickFailed 66_67L<Cent> ValueNone |]
                88u<OffsetDay>, [| 0, ActualPayment.quickFailed 66_67L<Cent> ValueNone |]
                88u<OffsetDay>, [| 1, ActualPayment.quickFailed 66_67L<Cent> ValueNone |]
                88u<OffsetDay>, [| 2, ActualPayment.quickFailed 66_67L<Cent> ValueNone |]
                91u<OffsetDay>, [| 0, ActualPayment.quickFailed 66_67L<Cent> ValueNone |]
                91u<OffsetDay>, [| 1, ActualPayment.quickFailed 66_67L<Cent> ValueNone |]
                91u<OffsetDay>, [| 2, ActualPayment.quickFailed 66_67L<Cent> ValueNone |]
                135u<OffsetDay>, [| 0, ActualPayment.quickConfirmed 83_33L<Cent> |]
                165u<OffsetDay>, [| 0, ActualPayment.quickConfirmed 83_33L<Cent> |]
                196u<OffsetDay>, [| 0, ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
                196u<OffsetDay>, [| 1, ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
                196u<OffsetDay>, [| 2, ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
                199u<OffsetDay>, [| 0, ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
                199u<OffsetDay>, [| 1, ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
                199u<OffsetDay>, [| 2, ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
                202u<OffsetDay>, [| 0, ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
                202u<OffsetDay>, [| 1, ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
                202u<OffsetDay>, [| 2, ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
                227u<OffsetDay>, [| 0, ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
                227u<OffsetDay>, [| 1, ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
                227u<OffsetDay>, [| 2, ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
                230u<OffsetDay>, [| 0, ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
                230u<OffsetDay>, [| 1, ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
                230u<OffsetDay>, [| 2, ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
                233u<OffsetDay>, [| 0, ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
                233u<OffsetDay>, [| 1, ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
                233u<OffsetDay>, [| 2, ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
                255u<OffsetDay>, [| 0, ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
                255u<OffsetDay>, [| 1, ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
                255u<OffsetDay>, [| 2, ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
                258u<OffsetDay>, [| 0, ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
                258u<OffsetDay>, [| 1, ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
                258u<OffsetDay>, [| 2, ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
                261u<OffsetDay>, [| 0, ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
                261u<OffsetDay>, [| 1, ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
                261u<OffsetDay>, [| 2, ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
                286u<OffsetDay>, [| 0, ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
                286u<OffsetDay>, [| 1, ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
                286u<OffsetDay>, [| 2, ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
                289u<OffsetDay>, [| 0, ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
                289u<OffsetDay>, [| 1, ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
                289u<OffsetDay>, [| 2, ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
                292u<OffsetDay>, [| 0, ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
                292u<OffsetDay>, [| 1, ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
                292u<OffsetDay>, [| 2, ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
                322u<OffsetDay>, [| 0, ActualPayment.quickConfirmed 17_58L<Cent> |]
                353u<OffsetDay>, [| 0, ActualPayment.quickConfirmed 17_58L<Cent> |]
                384u<OffsetDay>, [| 0, ActualPayment.quickConfirmed 17_58L<Cent> |]
                408u<OffsetDay>, [| 0, ActualPayment.quickConfirmed 17_58L<Cent> |]
                449u<OffsetDay>, [| 0, ActualPayment.quickConfirmed 17_58L<Cent> |]
                476u<OffsetDay>, [| 0, ActualPayment.quickConfirmed 17_58L<Cent> |]
                499u<OffsetDay>, [| 0, ActualPayment.quickConfirmed 15_74L<Cent> |]
                531u<OffsetDay>, [| 0, ActualPayment.quickConfirmed 15_74L<Cent> |]
                574u<OffsetDay>, [| 0, ActualPayment.quickConfirmed 15_74L<Cent> |]
                595u<OffsetDay>, [| 0, ActualPayment.quickConfirmed 15_74L<Cent> |]
                629u<OffsetDay>, [| 0, ActualPayment.quickConfirmed 15_74L<Cent> |]
            |]

        let actual =
            let quote = getQuote parameters2 actualPayments

            quote.Schedules
            |> Schedule.outputHtmlToFile folder title description parameters2 ""

            quote.QuoteResult

        let expected =
            PaymentQuote {
                PaymentValue = 64916L<Cent>
                Apportionment = {
                    PrincipalPortion = 500_00L<Cent>
                    FeePortion = 0L<Cent>
                    InterestPortion = 149_16L<Cent>
                    ChargesPortion = 0L<Cent>
                }
                FeeRebateIfSettled = 0L<Cent>
            }

        actual |> should equal expected

    [<Fact>]
    let EdgeCaseTest006 () =
        let title = "EdgeCaseTest006"
        let description = "Quote returning nothing"

        let p = {
            parameters2 with
                Basic.StartDate = Date(2021, 12, 26)
                Basic.Principal = 150000L<Cent>
                Basic.ScheduleConfig =
                    AutoGenerateSchedule {
                        UnitPeriodConfig = Monthly(1, 2022, 1, 7)
                        ScheduleLength = PaymentCount 6
                    }
        }

        let actualPayments =
            Map [
                12u<OffsetDay>,
                Map [
                    0700, ActualPayment.quickFailed 500_00L<Cent> ValueNone
                    0800, ActualPayment.quickFailed 500_00L<Cent> ValueNone
                ]
                15u<OffsetDay>,
                Map [
                    0700, ActualPayment.quickFailed 500_00L<Cent> ValueNone
                    0800, ActualPayment.quickFailed 500_00L<Cent> ValueNone
                    1514, ActualPayment.quickConfirmed 500_00L<Cent>
                ]
                43u<OffsetDay>,
                Map [
                    0700, ActualPayment.quickFailed 500_00L<Cent> ValueNone
                    0800, ActualPayment.quickFailed 500_00L<Cent> ValueNone
                ]
                45u<OffsetDay>, Map [ 1958, ActualPayment.quickConfirmed 1540_00L<Cent> ]
            ]

        let actual =
            let quote = getQuote p actualPayments

            quote.Schedules |> Schedule.outputHtmlToFile folder title description p ""

            quote.QuoteResult

        let expected =
            PaymentQuote {
                PaymentValue = -76_80L<Cent>
                Apportionment = {
                    PrincipalPortion = -76_80L<Cent>
                    FeePortion = 0L<Cent>
                    InterestPortion = 0L<Cent>
                    ChargesPortion = 0L<Cent>
                }
                FeeRebateIfSettled = 0L<Cent>
            }

        actual |> should equal expected


    [<Fact>]
    let EdgeCaseTest007 () =
        let title = "EdgeCaseTest007"
        let description = "Quote returning nothing"

        let p = {
            parameters2 with
                Basic.EvaluationDate = Date(2024, 4, 30)
                Basic.StartDate = Date(2024, 2, 2)
                Basic.Principal = 25000L<Cent>
                Basic.ScheduleConfig =
                    AutoGenerateSchedule {
                        UnitPeriodConfig = Monthly(1, 2024, 2, 22)
                        ScheduleLength = PaymentCount 4
                    }
        }

        let actualPayments =
            Map [
                6u<OffsetDay>, Map [ 070000, ActualPayment.quickFailed 2_00L<Cent> ValueNone ]
                16u<OffsetDay>,
                Map [
                    083013, ActualPayment.quickConfirmed 97_01L<Cent>
                    083014, ActualPayment.quickConfirmed 97_01L<Cent>
                ]
            ]

        let rescheduleDay = Date(2024, 3, 12) |> OffsetDay.fromDate p.Basic.StartDate

        let (rp: RescheduleParameters) = {
            FeeSettlementRebate = Fee.SettlementRebate.Zero
            PaymentSchedule =
                CustomSchedule
                <| Map [
                    58u<OffsetDay>,
                    ScheduledPayment.quick
                        ValueNone
                        (ValueSome {
                            Value = 5000L<Cent>
                            RescheduleDay = rescheduleDay
                        })
                ]
            RateOnNegativeBalance = Interest.Rate.Annual <| Percent 8m
            PromotionalInterestRates = [||]
            SettlementDay = SettlementDay.SettlementOnEvaluationDay
        }

        let schedules = reschedule p rp actualPayments

        schedules.NewSchedules
        |> Schedule.outputHtmlToFile folder title description p (RescheduleParameters.toHtmlTable rp)

        let actual =
            schedules.NewSchedules.AmortisationSchedule.ScheduleItems |> Map.maxKeyValue

        let expected =
            88u<OffsetDay>,
            {
                OffsetDayType = OffsetDayType.SettlementDay
                OffsetDate = Date(2024, 4, 30)
                Advances = [||]
                ScheduledPayment = ScheduledPayment.zero
                Window = 4
                PaymentDue = 0L<Cent>
                ActualPayments = Map.empty
                PaidBy = Map.empty
                GeneratedPayment = GeneratedValue 138_65L<Cent>
                NetEffect = 138_65L<Cent>
                PaymentStatus = Generated
                BalanceStatus = ClosedBalance
                ActuarialInterest = 5_63.072m<Cent>
                NewInterest = 5_63.072m<Cent>
                NewCharges = [||]
                PrincipalPortion = 87_98L<Cent>
                FeePortion = 0L<Cent>
                InterestPortion = 50_67L<Cent>
                ChargesPortion = 0L<Cent>
                FeeRebate = 0L<Cent>
                PrincipalBalance = 0L<Cent>
                FeeBalance = 0L<Cent>
                InterestBalance = 0m<Cent>
                ChargesBalance = 0L<Cent>
                SettlementFigure = 0L<Cent>
                FeeRebateIfSettled = 0L<Cent>
            }

        actual |> should equal expected

    [<Fact>]
    let EdgeCaseTest008 () =
        let title = "EdgeCaseTest008"
        let description = "Partial write-off"

        let p = {
            parameters2 with
                Basic.EvaluationDate = Date(2024, 4, 30)
                Basic.StartDate = Date(2024, 2, 2)
                Basic.Principal = 25000L<Cent>
                Basic.ScheduleConfig =
                    AutoGenerateSchedule {
                        UnitPeriodConfig = Monthly(1, 2024, 2, 22)
                        ScheduleLength = PaymentCount 4
                    }
        }

        let actualPayments =
            Map [
                6u<OffsetDay>, Map [ 070000, ActualPayment.quickWriteOff 42_00L<Cent> ]
                16u<OffsetDay>,
                Map [
                    083013, ActualPayment.quickConfirmed 97_01L<Cent>
                    083014, ActualPayment.quickConfirmed 97_01L<Cent>
                ]
            ]

        let rescheduleDay = Date(2024, 3, 12) |> OffsetDay.fromDate p.Basic.StartDate

        let (rp: RescheduleParameters) = {
            FeeSettlementRebate = Fee.SettlementRebate.Zero
            PaymentSchedule =
                CustomSchedule
                <| Map [
                    58u<OffsetDay>,
                    ScheduledPayment.quick
                        ValueNone
                        (ValueSome {
                            Value = 5000L<Cent>
                            RescheduleDay = rescheduleDay
                        })
                ]
            RateOnNegativeBalance = Interest.Rate.Annual <| Percent 8m
            PromotionalInterestRates = [||]
            SettlementDay = SettlementDay.SettlementOnEvaluationDay
        }

        let schedules = reschedule p rp actualPayments

        schedules.NewSchedules
        |> Schedule.outputHtmlToFile folder title description p (RescheduleParameters.toHtmlTable rp)

        let actual =
            schedules.NewSchedules.AmortisationSchedule.ScheduleItems |> Map.maxKeyValue

        let expected =
            88u<OffsetDay>,
            {
                OffsetDayType = OffsetDayType.SettlementDay
                OffsetDate = Date(2024, 4, 30)
                Advances = [||]
                ScheduledPayment = ScheduledPayment.zero
                Window = 4
                PaymentDue = 0L<Cent>
                ActualPayments = Map.empty
                PaidBy = Map.empty
                GeneratedPayment = GeneratedValue 68_68L<Cent>
                NetEffect = 68_68L<Cent>
                PaymentStatus = Generated
                BalanceStatus = ClosedBalance
                ActuarialInterest = 278.912m<Cent>
                NewInterest = 278.912m<Cent>
                NewCharges = [||]
                PrincipalPortion = 43_58L<Cent>
                FeePortion = 0L<Cent>
                InterestPortion = 25_10L<Cent>
                ChargesPortion = 0L<Cent>
                FeeRebate = 0L<Cent>
                PrincipalBalance = 0L<Cent>
                FeeBalance = 0L<Cent>
                InterestBalance = 0m<Cent>
                ChargesBalance = 0L<Cent>
                SettlementFigure = 0L<Cent>
                FeeRebateIfSettled = 0L<Cent>
            }

        actual |> should equal expected

    [<Fact>]
    let EdgeCaseTest009 () =
        let title = "EdgeCaseTest009"
        let description = "Negative principal balance accruing interest"

        let p = {
            parameters2 with
                Basic.EvaluationDate = Date(2024, 4, 5)
                Basic.StartDate = Date(2023, 5, 5)
                Basic.Principal = 25000L<Cent>
                Basic.ScheduleConfig =
                    AutoGenerateSchedule {
                        UnitPeriodConfig = Monthly(1, 2023, 5, 10)
                        ScheduleLength = PaymentCount 4
                    }
                Advanced.InterestConfig.RateOnNegativeBalance = Interest.Rate.Annual <| Percent 8m
        }

        let actualPayments =
            Map [
                5u<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 111_00L<Cent> ]
                21u<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 181_01L<Cent> ]
            ]

        let schedules = actualPayments |> amortise p

        Schedule.outputHtmlToFile folder title description p "" schedules

        let actual = schedules.AmortisationSchedule.ScheduleItems |> Map.maxKeyValue

        let expected =
            336u<OffsetDay>,
            {
                OffsetDayType = OffsetDayType.EvaluationDay
                OffsetDate = Date(2024, 4, 5)
                Advances = [||]
                ScheduledPayment = ScheduledPayment.zero
                Window = 11
                PaymentDue = 0L<Cent>
                ActualPayments = Map.empty
                PaidBy = Map.empty
                GeneratedPayment = NoGeneratedPayment
                NetEffect = 0L<Cent>
                PaymentStatus = InformationOnly
                BalanceStatus = RefundDue
                ActuarialInterest = -67.78432877m<Cent>
                NewInterest = -67.78432877m<Cent>
                NewCharges = [||]
                PrincipalPortion = 0L<Cent>
                FeePortion = 0L<Cent>
                InterestPortion = 0L<Cent>
                ChargesPortion = 0L<Cent>
                FeeRebate = 0L<Cent>
                PrincipalBalance = -12_94L<Cent>
                FeeBalance = 0L<Cent>
                InterestBalance = -89.3391781m<Cent>
                ChargesBalance = 0L<Cent>
                SettlementFigure = -13_84L<Cent>
                FeeRebateIfSettled = 0L<Cent>
            }

        actual |> should equal expected
