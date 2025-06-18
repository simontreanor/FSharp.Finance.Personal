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
                    15<OffsetDay>, ScheduledPayment.quick (ValueSome 137_40L<Cent>) ValueNone
                    43<OffsetDay>, ScheduledPayment.quick (ValueSome 137_40L<Cent>) ValueNone
                    74<OffsetDay>, ScheduledPayment.quick (ValueSome 137_40L<Cent>) ValueNone
                    104<OffsetDay>, ScheduledPayment.quick (ValueSome 137_40L<Cent>) ValueNone
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
                AprPrecision = 5
                Rounding = RoundDown
            }
        }
        Advanced = {
            PaymentConfig = {
                ScheduledPaymentOption = AsScheduled
                Minimum = DeferOrWriteOff 50L<Cent>
                Timeout = 3<OffsetDay>
            }
            FeeConfig =
                ValueSome {
                    SettlementRebate = Fee.SettlementRebate.ProRata
                }
            ChargeConfig = None
            InterestConfig = {
                InitialGracePeriod = 3<OffsetDay>
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
            Map [ 5<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 31200L<Cent> ] ]

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
                        16<OffsetDay>, ScheduledPayment.quick (ValueSome 11500L<Cent>) ValueNone
                        44<OffsetDay>, ScheduledPayment.quick (ValueSome 11500L<Cent>) ValueNone
                        75<OffsetDay>, ScheduledPayment.quick (ValueSome 11500L<Cent>) ValueNone
                        105<OffsetDay>, ScheduledPayment.quick (ValueSome 11500L<Cent>) ValueNone
                    ]
        }

        let actualPayments =
            Map [ 5<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 26000L<Cent> ] ]

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
                        14<OffsetDay>, ScheduledPayment.quick (ValueSome 34350L<Cent>) ValueNone
                        45<OffsetDay>, ScheduledPayment.quick (ValueSome 34350L<Cent>) ValueNone
                        76<OffsetDay>, ScheduledPayment.quick (ValueSome 34350L<Cent>) ValueNone
                        104<OffsetDay>, ScheduledPayment.quick (ValueSome 34350L<Cent>) ValueNone
                    ]
        }

        let actualPayments =
            Map [ 13<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 82800L<Cent> ] ]

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
                        8<OffsetDay>, ScheduledPayment.quick (ValueSome 22500L<Cent>) ValueNone
                        39<OffsetDay>, ScheduledPayment.quick (ValueSome 22500L<Cent>) ValueNone
                        69<OffsetDay>, ScheduledPayment.quick (ValueSome 22500L<Cent>) ValueNone
                        100<OffsetDay>, ScheduledPayment.quick (ValueSome 22500L<Cent>) ValueNone
                        214<OffsetDay>, ScheduledPayment.quick (ValueSome 25000L<Cent>) ValueNone
                        245<OffsetDay>, ScheduledPayment.quick (ValueSome 27600L<Cent>) ValueNone
                    ]
        }

        let actualPayments =
            Map.merge [|
                8<OffsetDay>, [| 0, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                8<OffsetDay>, [| 1, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                8<OffsetDay>, [| 2, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                11<OffsetDay>, [| 0, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                11<OffsetDay>, [| 1, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                11<OffsetDay>, [| 2, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                14<OffsetDay>, [| 0, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                14<OffsetDay>, [| 1, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                14<OffsetDay>, [| 2, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                39<OffsetDay>, [| 0, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                39<OffsetDay>, [| 1, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                39<OffsetDay>, [| 2, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                42<OffsetDay>, [| 0, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                42<OffsetDay>, [| 1, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                42<OffsetDay>, [| 2, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                45<OffsetDay>, [| 0, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                45<OffsetDay>, [| 1, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                45<OffsetDay>, [| 2, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                69<OffsetDay>, [| 0, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                69<OffsetDay>, [| 1, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                69<OffsetDay>, [| 2, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                72<OffsetDay>, [| 0, ActualPayment.quickConfirmed 22500L<Cent> |]
                72<OffsetDay>, [| 1, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                72<OffsetDay>, [| 2, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                75<OffsetDay>, [| 0, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                75<OffsetDay>, [| 1, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                75<OffsetDay>, [| 2, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                100<OffsetDay>, [| 0, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                100<OffsetDay>, [| 1, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                100<OffsetDay>, [| 2, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                103<OffsetDay>, [| 0, ActualPayment.quickFailed 23700L<Cent> ValueNone |]
                103<OffsetDay>, [| 1, ActualPayment.quickFailed 23700L<Cent> ValueNone |]
                103<OffsetDay>, [| 2, ActualPayment.quickFailed 23700L<Cent> ValueNone |]
                106<OffsetDay>, [| 0, ActualPayment.quickConfirmed 24900L<Cent> |]
                106<OffsetDay>, [| 1, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                106<OffsetDay>, [| 2, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                214<OffsetDay>, [| 0, ActualPayment.quickFailed 25000L<Cent> ValueNone |]
                214<OffsetDay>, [| 1, ActualPayment.quickFailed 25000L<Cent> ValueNone |]
                214<OffsetDay>, [| 2, ActualPayment.quickFailed 25000L<Cent> ValueNone |]
                217<OffsetDay>, [| 0, ActualPayment.quickFailed 25000L<Cent> ValueNone |]
                217<OffsetDay>, [| 1, ActualPayment.quickFailed 25000L<Cent> ValueNone |]
                217<OffsetDay>, [| 2, ActualPayment.quickFailed 25000L<Cent> ValueNone |]
                220<OffsetDay>, [| 0, ActualPayment.quickFailed 25000L<Cent> ValueNone |]
                220<OffsetDay>, [| 1, ActualPayment.quickFailed 25000L<Cent> ValueNone |]
                220<OffsetDay>, [| 2, ActualPayment.quickFailed 25000L<Cent> ValueNone |]
                245<OffsetDay>, [| 0, ActualPayment.quickFailed 25000L<Cent> ValueNone |]
                245<OffsetDay>, [| 1, ActualPayment.quickFailed 25000L<Cent> ValueNone |]
                245<OffsetDay>, [| 2, ActualPayment.quickFailed 25000L<Cent> ValueNone |]
                245<OffsetDay>, [| 3, ActualPayment.quickFailed 25000L<Cent> ValueNone |]
                248<OffsetDay>, [| 0, ActualPayment.quickFailed 25000L<Cent> ValueNone |]
                248<OffsetDay>, [| 1, ActualPayment.quickFailed 25000L<Cent> ValueNone |]
                248<OffsetDay>, [| 2, ActualPayment.quickFailed 25000L<Cent> ValueNone |]
                251<OffsetDay>, [| 0, ActualPayment.quickFailed 25000L<Cent> ValueNone |]
                251<OffsetDay>, [| 1, ActualPayment.quickFailed 25000L<Cent> ValueNone |]
                251<OffsetDay>, [| 2, ActualPayment.quickFailed 25000L<Cent> ValueNone |]
                379<OffsetDay>, [| 0, ActualPayment.quickFailed 17500L<Cent> ValueNone |]
                379<OffsetDay>, [| 1, ActualPayment.quickFailed 17500L<Cent> ValueNone |]
                379<OffsetDay>, [| 2, ActualPayment.quickFailed 17500L<Cent> ValueNone |]
                380<OffsetDay>, [| 0, ActualPayment.quickConfirmed 17500L<Cent> |]
                407<OffsetDay>, [| 0, ActualPayment.quickConfirmed 17500L<Cent> |]
                435<OffsetDay>, [| 0, ActualPayment.quickFailed 17600L<Cent> ValueNone |]
                435<OffsetDay>, [| 1, ActualPayment.quickFailed 17600L<Cent> ValueNone |]
                435<OffsetDay>, [| 2, ActualPayment.quickFailed 17600L<Cent> ValueNone |]
                435<OffsetDay>, [| 3, ActualPayment.quickFailed 17600L<Cent> ValueNone |]
                438<OffsetDay>, [| 0, ActualPayment.quickFailed 17600L<Cent> ValueNone |]
                438<OffsetDay>, [| 1, ActualPayment.quickFailed 17600L<Cent> ValueNone |]
                438<OffsetDay>, [| 2, ActualPayment.quickFailed 17600L<Cent> ValueNone |]
                441<OffsetDay>, [| 0, ActualPayment.quickFailed 17600L<Cent> ValueNone |]
                441<OffsetDay>, [| 1, ActualPayment.quickFailed 17600L<Cent> ValueNone |]
                441<OffsetDay>, [| 2, ActualPayment.quickFailed 17600L<Cent> ValueNone |]
                475<OffsetDay>, [| 0, ActualPayment.quickConfirmed 17600L<Cent> |]
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
                        8<OffsetDay>, ScheduledPayment.quick (ValueSome 22500L<Cent>) ValueNone
                        39<OffsetDay>, ScheduledPayment.quick (ValueSome 22500L<Cent>) ValueNone
                        69<OffsetDay>, ScheduledPayment.quick (ValueSome 22500L<Cent>) ValueNone
                        100<OffsetDay>, ScheduledPayment.quick (ValueSome 22500L<Cent>) ValueNone
                        214<OffsetDay>, ScheduledPayment.quick (ValueSome 25000L<Cent>) ValueNone
                        245<OffsetDay>, ScheduledPayment.quick (ValueSome 27600L<Cent>) ValueNone
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
                8<OffsetDay>,
                [|
                    0, ActualPayment.quickFailed 22500L<Cent> (ValueSome Charge.InsufficientFunds)
                |]
                8<OffsetDay>,
                [|
                    1, ActualPayment.quickFailed 22500L<Cent> (ValueSome Charge.InsufficientFunds)
                |]
                8<OffsetDay>,
                [|
                    2, ActualPayment.quickFailed 22500L<Cent> (ValueSome Charge.InsufficientFunds)
                |]
                11<OffsetDay>, [| 0, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                11<OffsetDay>, [| 1, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                11<OffsetDay>, [| 2, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                14<OffsetDay>, [| 0, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                14<OffsetDay>, [| 1, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                14<OffsetDay>, [| 2, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                39<OffsetDay>, [| 0, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                39<OffsetDay>, [| 1, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                39<OffsetDay>, [| 2, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                42<OffsetDay>, [| 0, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                42<OffsetDay>, [| 1, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                42<OffsetDay>, [| 2, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                45<OffsetDay>, [| 0, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                45<OffsetDay>, [| 1, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                45<OffsetDay>, [| 2, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                69<OffsetDay>, [| 0, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                69<OffsetDay>, [| 1, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                69<OffsetDay>, [| 2, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                72<OffsetDay>, [| 0, ActualPayment.quickConfirmed 22500L<Cent> |]
                72<OffsetDay>, [| 1, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                72<OffsetDay>, [| 2, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                75<OffsetDay>, [| 0, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                75<OffsetDay>, [| 1, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                75<OffsetDay>, [| 2, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                100<OffsetDay>, [| 0, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                100<OffsetDay>, [| 1, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                100<OffsetDay>, [| 2, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                103<OffsetDay>, [| 0, ActualPayment.quickFailed 23700L<Cent> ValueNone |]
                103<OffsetDay>, [| 1, ActualPayment.quickFailed 23700L<Cent> ValueNone |]
                103<OffsetDay>, [| 2, ActualPayment.quickFailed 23700L<Cent> ValueNone |]
                106<OffsetDay>, [| 0, ActualPayment.quickConfirmed 24900L<Cent> |]
                106<OffsetDay>, [| 1, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                106<OffsetDay>, [| 2, ActualPayment.quickFailed 22500L<Cent> ValueNone |]
                214<OffsetDay>, [| 0, ActualPayment.quickFailed 25000L<Cent> ValueNone |]
                214<OffsetDay>, [| 1, ActualPayment.quickFailed 25000L<Cent> ValueNone |]
                214<OffsetDay>, [| 2, ActualPayment.quickFailed 25000L<Cent> ValueNone |]
                217<OffsetDay>, [| 0, ActualPayment.quickFailed 25000L<Cent> ValueNone |]
                217<OffsetDay>, [| 1, ActualPayment.quickFailed 25000L<Cent> ValueNone |]
                217<OffsetDay>, [| 2, ActualPayment.quickFailed 25000L<Cent> ValueNone |]
                220<OffsetDay>, [| 0, ActualPayment.quickFailed 25000L<Cent> ValueNone |]
                220<OffsetDay>, [| 1, ActualPayment.quickFailed 25000L<Cent> ValueNone |]
                220<OffsetDay>, [| 2, ActualPayment.quickFailed 25000L<Cent> ValueNone |]
                245<OffsetDay>, [| 0, ActualPayment.quickFailed 25000L<Cent> ValueNone |]
                245<OffsetDay>, [| 1, ActualPayment.quickFailed 25000L<Cent> ValueNone |]
                245<OffsetDay>, [| 2, ActualPayment.quickFailed 25000L<Cent> ValueNone |]
                245<OffsetDay>, [| 3, ActualPayment.quickFailed 25000L<Cent> ValueNone |]
                248<OffsetDay>, [| 0, ActualPayment.quickFailed 25000L<Cent> ValueNone |]
                248<OffsetDay>, [| 1, ActualPayment.quickFailed 25000L<Cent> ValueNone |]
                248<OffsetDay>, [| 2, ActualPayment.quickFailed 25000L<Cent> ValueNone |]
                251<OffsetDay>, [| 0, ActualPayment.quickFailed 25000L<Cent> ValueNone |]
                251<OffsetDay>, [| 1, ActualPayment.quickFailed 25000L<Cent> ValueNone |]
                251<OffsetDay>, [| 2, ActualPayment.quickFailed 25000L<Cent> ValueNone |]
                379<OffsetDay>, [| 0, ActualPayment.quickFailed 17500L<Cent> ValueNone |]
                379<OffsetDay>, [| 1, ActualPayment.quickFailed 17500L<Cent> ValueNone |]
                379<OffsetDay>, [| 2, ActualPayment.quickFailed 17500L<Cent> ValueNone |]
                380<OffsetDay>, [| 0, ActualPayment.quickConfirmed 17500L<Cent> |]
                407<OffsetDay>, [| 0, ActualPayment.quickConfirmed 17500L<Cent> |]
                435<OffsetDay>, [| 0, ActualPayment.quickFailed 17600L<Cent> ValueNone |]
                435<OffsetDay>, [| 1, ActualPayment.quickFailed 17600L<Cent> ValueNone |]
                435<OffsetDay>, [| 2, ActualPayment.quickFailed 17600L<Cent> ValueNone |]
                435<OffsetDay>, [| 3, ActualPayment.quickFailed 17600L<Cent> ValueNone |]
                438<OffsetDay>, [| 0, ActualPayment.quickFailed 17600L<Cent> ValueNone |]
                438<OffsetDay>, [| 1, ActualPayment.quickFailed 17600L<Cent> ValueNone |]
                438<OffsetDay>, [| 2, ActualPayment.quickFailed 17600L<Cent> ValueNone |]
                441<OffsetDay>, [| 0, ActualPayment.quickFailed 17600L<Cent> ValueNone |]
                441<OffsetDay>, [| 1, ActualPayment.quickFailed 17600L<Cent> ValueNone |]
                441<OffsetDay>, [| 2, ActualPayment.quickFailed 17600L<Cent> ValueNone |]
                475<OffsetDay>, [| 0, ActualPayment.quickConfirmed 17600L<Cent> |]
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
                AprPrecision = 3
                Rounding = RoundDown
            }
            Advanced.FeeConfig = ValueNone
            Advanced.InterestConfig = {
                parameters1.Advanced.InterestConfig with
                    InitialGracePeriod = 0<OffsetDay>
            }
    }

    [<Fact>]
    let EdgeCaseTest005 () =
        let title = "EdgeCaseTest005"
        let description = "Quote returning nothing"

        let actualPayments =
            Map.merge [|
                23<OffsetDay>, [| 0, ActualPayment.quickFailed 166_67L<Cent> ValueNone |]
                23<OffsetDay>, [| 1, ActualPayment.quickFailed 66_67L<Cent> ValueNone |]
                23<OffsetDay>, [| 2, ActualPayment.quickFailed 66_67L<Cent> ValueNone |]
                26<OffsetDay>, [| 0, ActualPayment.quickFailed 66_67L<Cent> ValueNone |]
                26<OffsetDay>, [| 1, ActualPayment.quickFailed 66_67L<Cent> ValueNone |]
                26<OffsetDay>, [| 2, ActualPayment.quickFailed 66_67L<Cent> ValueNone |]
                29<OffsetDay>, [| 0, ActualPayment.quickFailed 66_67L<Cent> ValueNone |]
                29<OffsetDay>, [| 1, ActualPayment.quickFailed 66_67L<Cent> ValueNone |]
                29<OffsetDay>, [| 2, ActualPayment.quickFailed 66_67L<Cent> ValueNone |]
                54<OffsetDay>, [| 0, ActualPayment.quickFailed 66_67L<Cent> ValueNone |]
                54<OffsetDay>, [| 1, ActualPayment.quickFailed 66_67L<Cent> ValueNone |]
                54<OffsetDay>, [| 2, ActualPayment.quickFailed 66_67L<Cent> ValueNone |]
                57<OffsetDay>, [| 0, ActualPayment.quickFailed 66_67L<Cent> ValueNone |]
                57<OffsetDay>, [| 1, ActualPayment.quickFailed 66_67L<Cent> ValueNone |]
                57<OffsetDay>, [| 2, ActualPayment.quickFailed 66_67L<Cent> ValueNone |]
                60<OffsetDay>, [| 0, ActualPayment.quickFailed 66_67L<Cent> ValueNone |]
                60<OffsetDay>, [| 1, ActualPayment.quickFailed 66_67L<Cent> ValueNone |]
                60<OffsetDay>, [| 2, ActualPayment.quickFailed 66_67L<Cent> ValueNone |]
                85<OffsetDay>, [| 0, ActualPayment.quickFailed 66_67L<Cent> ValueNone |]
                85<OffsetDay>, [| 1, ActualPayment.quickFailed 66_67L<Cent> ValueNone |]
                85<OffsetDay>, [| 2, ActualPayment.quickFailed 66_67L<Cent> ValueNone |]
                88<OffsetDay>, [| 0, ActualPayment.quickFailed 66_67L<Cent> ValueNone |]
                88<OffsetDay>, [| 1, ActualPayment.quickFailed 66_67L<Cent> ValueNone |]
                88<OffsetDay>, [| 2, ActualPayment.quickFailed 66_67L<Cent> ValueNone |]
                91<OffsetDay>, [| 0, ActualPayment.quickFailed 66_67L<Cent> ValueNone |]
                91<OffsetDay>, [| 1, ActualPayment.quickFailed 66_67L<Cent> ValueNone |]
                91<OffsetDay>, [| 2, ActualPayment.quickFailed 66_67L<Cent> ValueNone |]
                135<OffsetDay>, [| 0, ActualPayment.quickConfirmed 83_33L<Cent> |]
                165<OffsetDay>, [| 0, ActualPayment.quickConfirmed 83_33L<Cent> |]
                196<OffsetDay>, [| 0, ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
                196<OffsetDay>, [| 1, ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
                196<OffsetDay>, [| 2, ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
                199<OffsetDay>, [| 0, ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
                199<OffsetDay>, [| 1, ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
                199<OffsetDay>, [| 2, ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
                202<OffsetDay>, [| 0, ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
                202<OffsetDay>, [| 1, ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
                202<OffsetDay>, [| 2, ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
                227<OffsetDay>, [| 0, ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
                227<OffsetDay>, [| 1, ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
                227<OffsetDay>, [| 2, ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
                230<OffsetDay>, [| 0, ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
                230<OffsetDay>, [| 1, ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
                230<OffsetDay>, [| 2, ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
                233<OffsetDay>, [| 0, ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
                233<OffsetDay>, [| 1, ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
                233<OffsetDay>, [| 2, ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
                255<OffsetDay>, [| 0, ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
                255<OffsetDay>, [| 1, ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
                255<OffsetDay>, [| 2, ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
                258<OffsetDay>, [| 0, ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
                258<OffsetDay>, [| 1, ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
                258<OffsetDay>, [| 2, ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
                261<OffsetDay>, [| 0, ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
                261<OffsetDay>, [| 1, ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
                261<OffsetDay>, [| 2, ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
                286<OffsetDay>, [| 0, ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
                286<OffsetDay>, [| 1, ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
                286<OffsetDay>, [| 2, ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
                289<OffsetDay>, [| 0, ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
                289<OffsetDay>, [| 1, ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
                289<OffsetDay>, [| 2, ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
                292<OffsetDay>, [| 0, ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
                292<OffsetDay>, [| 1, ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
                292<OffsetDay>, [| 2, ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
                322<OffsetDay>, [| 0, ActualPayment.quickConfirmed 17_58L<Cent> |]
                353<OffsetDay>, [| 0, ActualPayment.quickConfirmed 17_58L<Cent> |]
                384<OffsetDay>, [| 0, ActualPayment.quickConfirmed 17_58L<Cent> |]
                408<OffsetDay>, [| 0, ActualPayment.quickConfirmed 17_58L<Cent> |]
                449<OffsetDay>, [| 0, ActualPayment.quickConfirmed 17_58L<Cent> |]
                476<OffsetDay>, [| 0, ActualPayment.quickConfirmed 17_58L<Cent> |]
                499<OffsetDay>, [| 0, ActualPayment.quickConfirmed 15_74L<Cent> |]
                531<OffsetDay>, [| 0, ActualPayment.quickConfirmed 15_74L<Cent> |]
                574<OffsetDay>, [| 0, ActualPayment.quickConfirmed 15_74L<Cent> |]
                595<OffsetDay>, [| 0, ActualPayment.quickConfirmed 15_74L<Cent> |]
                629<OffsetDay>, [| 0, ActualPayment.quickConfirmed 15_74L<Cent> |]
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
                12<OffsetDay>,
                Map [
                    0700, ActualPayment.quickFailed 500_00L<Cent> ValueNone
                    0800, ActualPayment.quickFailed 500_00L<Cent> ValueNone
                ]
                15<OffsetDay>,
                Map [
                    0700, ActualPayment.quickFailed 500_00L<Cent> ValueNone
                    0800, ActualPayment.quickFailed 500_00L<Cent> ValueNone
                    1514, ActualPayment.quickConfirmed 500_00L<Cent>
                ]
                43<OffsetDay>,
                Map [
                    0700, ActualPayment.quickFailed 500_00L<Cent> ValueNone
                    0800, ActualPayment.quickFailed 500_00L<Cent> ValueNone
                ]
                45<OffsetDay>, Map [ 1958, ActualPayment.quickConfirmed 1540_00L<Cent> ]
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
                6<OffsetDay>, Map [ 070000, ActualPayment.quickFailed 2_00L<Cent> ValueNone ]
                16<OffsetDay>,
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
                    58<OffsetDay>,
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
            88<OffsetDay>,
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
                6<OffsetDay>, Map [ 070000, ActualPayment.quickWriteOff 42_00L<Cent> ]
                16<OffsetDay>,
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
                    58<OffsetDay>,
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
            88<OffsetDay>,
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
                5<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 111_00L<Cent> ]
                21<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 181_01L<Cent> ]
            ]

        let schedules = actualPayments |> amortise p

        Schedule.outputHtmlToFile folder title description p "" schedules

        let actual = schedules.AmortisationSchedule.ScheduleItems |> Map.maxKeyValue

        let expected =
            336<OffsetDay>,
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
