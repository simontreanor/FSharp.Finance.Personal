namespace FSharp.Finance.Personal.Tests

open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal

module UnitPeriodConfigTests =

    open Amortisation
    open Calculation
    open DateDay
    open Scheduling
    open Quotes

    open UnitPeriod
    module DefaultConfig =

        [<Fact>]
        let ``Default semi-monthly function produces valid unit-period configs`` () =
            let actual =
                [| 0 .. 365 * 5 |]
                |> Array.choose(fun d ->
                    let config = d |> Date(2024, 2, 13).AddDays |> Config.defaultSemiMonthly
                    try
                        Config.constrain config |> ignore
                        None
                    with
                        | ex -> Some ex.Message
                )
            let expected = [||]
            actual |> should equal expected

        [<Fact>]
        let ``Default monthly function produces valid unit-period configs`` () =
            let actual =
                [| 0 .. 365 * 5 |]
                |> Array.choose(fun d ->
                    let config = d |> Date(2024, 2, 13).AddDays |> Config.defaultMonthly 1
                    try
                        Config.constrain config |> ignore
                        None
                    with
                        | ex -> Some ex.Message
                )
            let expected = [||]
            actual |> should equal expected

    module ConfigEdges =

        [<Fact>]
        let UnitPeriodConfigTest000 () =
            let title = "UnitPeriodConfigTest000"
            let description = "Irregular payment schedule does not break detect function"
            let sp = {
                    AsOfDate = Date(2024, 3, 5)
                    StartDate = Date(2022, 5, 5)
                    Principal = 100000L<Cent>
                    ScheduleConfig = AutoGenerateSchedule { UnitPeriodConfig = Weekly(2, Date(2022, 5, 13)); PaymentCount = 12; MaxDuration = Duration.Unlimited }
                    PaymentConfig = {
                        LevelPaymentOption = LowerFinalPayment
                        ScheduledPaymentOption = AsScheduled
                        CloseBalanceOption = LeaveOpenBalance
                        PaymentRounding = RoundUp
                        MinimumPayment = DeferOrWriteOff 50L<Cent>
                        PaymentTimeout = 3<DurationDay>
                    }
                    FeeConfig = Some {
                        FeeTypes = [| Fee.FeeType.CabOrCsoFee (Amount.Percentage (Percent 154.47m, Restriction.NoLimit)) |]
                        Rounding = RoundDown
                        FeeAmortisation = Fee.FeeAmortisation.AmortiseProportionately
                        SettlementRefund = Fee.SettlementRefund.ProRata
                    }
                    ChargeConfig = None
                    InterestConfig = {
                        Method = Interest.Method.Simple
                        StandardRate = Interest.Rate.Annual (Percent 9.95m)
                        Cap = Interest.Cap.Zero
                        InitialGracePeriod = 3<DurationDay>
                        PromotionalRates = [||]
                        RateOnNegativeBalance = Interest.Rate.Zero
                        AprMethod = Apr.CalculationMethod.UsActuarial 5
                        Rounding = RoundDown
                    }
                }
            
            let actualPayments =
                Map [
                    8<OffsetDay>, [| ActualPayment.quickConfirmed 21700L<Cent> |]
                    22<OffsetDay>, [| ActualPayment.quickFailed 21700L<Cent> ValueNone; ActualPayment.quickFailed 21700L<Cent> ValueNone; ActualPayment.quickFailed 21700L<Cent> ValueNone |]
                    25<OffsetDay>, [| ActualPayment.quickConfirmed 21700L<Cent> |]
                    39<OffsetDay>, [| ActualPayment.quickFailed 21700L<Cent> ValueNone; ActualPayment.quickFailed 21700L<Cent> ValueNone; ActualPayment.quickFailed 21700L<Cent> ValueNone |]
                    45<OffsetDay>, [| ActualPayment.quickFailed 21700L<Cent> ValueNone; ActualPayment.quickConfirmed 21700L<Cent> |]
                    50<OffsetDay>, [| ActualPayment.quickFailed 21700L<Cent> ValueNone; ActualPayment.quickFailed 21700L<Cent> ValueNone; ActualPayment.quickFailed 21700L<Cent> ValueNone |]
                    53<OffsetDay>, [| ActualPayment.quickFailed 21700L<Cent> ValueNone; ActualPayment.quickFailed 21700L<Cent> ValueNone; ActualPayment.quickFailed 21700L<Cent> ValueNone |]
                    56<OffsetDay>, [| ActualPayment.quickConfirmed 21700L<Cent> |]
                    67<OffsetDay>, [| ActualPayment.quickFailed 21700L<Cent> ValueNone; ActualPayment.quickFailed 21700L<Cent> ValueNone; ActualPayment.quickFailed 21700L<Cent> ValueNone |]
                    73<OffsetDay>, [| ActualPayment.quickFailed 21700L<Cent> ValueNone; ActualPayment.quickFailed 21700L<Cent> ValueNone; ActualPayment.quickFailed 21700L<Cent> ValueNone |]
                    78<OffsetDay>, [| ActualPayment.quickFailed 21700L<Cent> ValueNone; ActualPayment.quickFailed 21700L<Cent> ValueNone; ActualPayment.quickFailed 21700L<Cent> ValueNone |]
                    79<OffsetDay>, [| ActualPayment.quickFailed 21700L<Cent> ValueNone; ActualPayment.quickFailed 21700L<Cent> ValueNone; ActualPayment.quickFailed 21700L<Cent> ValueNone |]
                    274<OffsetDay>, [| ActualPayment.quickFailed 26036L<Cent> ValueNone |]
                    302<OffsetDay>, [| ActualPayment.quickFailed 26036L<Cent> ValueNone |]
                    330<OffsetDay>, [| ActualPayment.quickConfirmed 21700L<Cent> |]
                ]

            let actual =
                let quote = getQuote SettlementDay.SettlementOnAsOfDay sp actualPayments
                quote.RevisedSchedules |> Schedule.outputHtmlToFile title description sp
                quote.RevisedSchedules.AmortisationSchedule.ScheduleStats.FinalApr

            let expected = Percent 56.513m
            actual |> should equal expected

        [<Fact>]
        let UnitPeriodConfigTest001 () =
            let title = "UnitPeriodConfigTest001"
            let description = "Irregular payment schedule does not break APR calculation"
            let sp = {
                AsOfDate = Date(2024, 3, 5)
                StartDate = Date(2023, 4, 13)
                Principal = 70000L<Cent>
                ScheduleConfig = AutoGenerateSchedule { UnitPeriodConfig = Weekly(2, Date(2023, 4, 20)); PaymentCount = 12; MaxDuration = Duration.Unlimited }
                PaymentConfig = {
                    LevelPaymentOption = LowerFinalPayment
                    ScheduledPaymentOption = AsScheduled
                    CloseBalanceOption = LeaveOpenBalance
                    PaymentRounding = RoundUp
                    MinimumPayment = DeferOrWriteOff 50L<Cent>
                    PaymentTimeout = 3<DurationDay>
                }
                FeeConfig = Some {
                    FeeTypes = [| Fee.FeeType.CabOrCsoFee (Amount.Percentage (Percent 154.47m, Restriction.NoLimit)) |]
                    Rounding = RoundDown
                    FeeAmortisation = Fee.FeeAmortisation.AmortiseProportionately
                    SettlementRefund = Fee.SettlementRefund.ProRata
                }
                ChargeConfig = None
                InterestConfig = {
                    Method = Interest.Method.Simple
                    StandardRate = Interest.Rate.Annual (Percent 9.95m)
                    Cap = Interest.Cap.Zero
                    InitialGracePeriod = 3<DurationDay>
                    PromotionalRates = [||]
                    RateOnNegativeBalance = Interest.Rate.Zero
                    AprMethod = Apr.CalculationMethod.UsActuarial 5
                    Rounding = RoundDown
                }
            }

            let actualPayments =
                Map [
                    6<OffsetDay>, [| ActualPayment.quickConfirmed 17275L<Cent> |]
                    21<OffsetDay>, [| ActualPayment.quickConfirmed 17275L<Cent> |]
                    35<OffsetDay>, [| ActualPayment.quickConfirmed 17275L<Cent> |]
                    49<OffsetDay>, [| ActualPayment.quickFailed 17275L<Cent> ValueNone; ActualPayment.quickFailed 17275L<Cent> ValueNone |]
                    50<OffsetDay>, [| ActualPayment.quickConfirmed 17275L<Cent> |]
                    64<OffsetDay>, [| ActualPayment.quickConfirmed 17275L<Cent> |]
                    80<OffsetDay>, [| ActualPayment.quickConfirmed 17275L<Cent> |]
                    94<OffsetDay>, [| ActualPayment.quickConfirmed 17488L<Cent> |]
                    108<OffsetDay>, [| ActualPayment.quickFailed 17488L<Cent> ValueNone; ActualPayment.quickFailed 17488L<Cent> ValueNone |]
                    109<OffsetDay>, [| ActualPayment.quickFailed 17488L<Cent> ValueNone |]
                    111<OffsetDay>, [| ActualPayment.quickFailed 17701L<Cent> ValueNone; ActualPayment.quickFailed 17701L<Cent> ValueNone |]
                    112<OffsetDay>, [| ActualPayment.quickFailed 17772L<Cent> ValueNone; ActualPayment.quickFailed 17772L<Cent> ValueNone; ActualPayment.quickConfirmed 17772L<Cent> |]
                    122<OffsetDay>, [| ActualPayment.quickConfirmed 17488L<Cent> |]
                    128<OffsetDay>, [| ActualPayment.quickConfirmed 23521L<Cent> |]
                ]

            let actual =
                let quote = getQuote SettlementDay.SettlementOnAsOfDay sp actualPayments
                quote.RevisedSchedules |> Schedule.outputHtmlToFile title description sp
                quote.RevisedSchedules.AmortisationSchedule.ScheduleStats.FinalApr

            let expected = Percent 986.813m

            actual |> should equal expected

        [<Fact>]
        let UnitPeriodConfigTest002 () =
            let title = "UnitPeriodConfigTest002"
            let description = "Irregular payment schedule does not break APR calculation"
            let sp = {
                AsOfDate = Date(2024, 3, 5)
                StartDate = Date(2023, 1, 20)
                Principal = 65000L<Cent>
                ScheduleConfig = AutoGenerateSchedule {
                    UnitPeriodConfig = Weekly(2, Date(2023, 2, 2))
                    PaymentCount = 11
                    MaxDuration = Duration.Unlimited
                }
                PaymentConfig = {
                    LevelPaymentOption = LowerFinalPayment
                    ScheduledPaymentOption = AsScheduled
                    CloseBalanceOption = LeaveOpenBalance
                    PaymentRounding = RoundUp
                    MinimumPayment = DeferOrWriteOff 50L<Cent>
                    PaymentTimeout = 3<DurationDay>
                }
                FeeConfig = Some {
                    FeeTypes = [| Fee.FeeType.CabOrCsoFee (Amount.Percentage (Percent 154.47m, Restriction.NoLimit)) |]
                    Rounding = RoundDown
                    FeeAmortisation = Fee.FeeAmortisation.AmortiseProportionately
                    SettlementRefund = Fee.SettlementRefund.ProRata
                }
                ChargeConfig = None
                InterestConfig = {
                    Method = Interest.Method.Simple
                    StandardRate = Interest.Rate.Annual (Percent 9.95m)
                    Cap = Interest.Cap.Zero
                    InitialGracePeriod = 3<DurationDay>
                    PromotionalRates = [||]
                    RateOnNegativeBalance = Interest.Rate.Zero
                    AprMethod = Apr.CalculationMethod.UsActuarial 5
                    Rounding = RoundDown
                }
            }

            let actualPayments =
                Map [
                    13<OffsetDay>, [| ActualPayment.quickFailed 17494L<Cent> ValueNone; ActualPayment.quickFailed 17494L<Cent> ValueNone |]
                    14<OffsetDay>, [| ActualPayment.quickFailed 17494L<Cent> ValueNone |]
                    16<OffsetDay>, [| ActualPayment.quickFailed 17494L<Cent> ValueNone; ActualPayment.quickFailed 17494L<Cent> ValueNone |]
                    17<OffsetDay>, [| ActualPayment.quickFailed 17494L<Cent> ValueNone |]
                    19<OffsetDay>, [| ActualPayment.quickConfirmed 17494L<Cent> |]
                    27<OffsetDay>, [| ActualPayment.quickFailed 17494L<Cent> ValueNone; ActualPayment.quickFailed 17494L<Cent> ValueNone |]
                    28<OffsetDay>, [| ActualPayment.quickFailed 17494L<Cent> ValueNone |]
                    30<OffsetDay>, [| ActualPayment.quickFailed 17494L<Cent> ValueNone; ActualPayment.quickFailed 17494L<Cent> ValueNone |]
                    31<OffsetDay>, [| ActualPayment.quickFailed 17494L<Cent> ValueNone |]
                    33<OffsetDay>, [| ActualPayment.quickFailed 17494L<Cent> ValueNone; ActualPayment.quickFailed 17494L<Cent> ValueNone |]
                    34<OffsetDay>, [| ActualPayment.quickFailed 17494L<Cent> ValueNone |]
                    36<OffsetDay>, [| ActualPayment.quickConfirmed 17494L<Cent> |]
                    41<OffsetDay>, [| ActualPayment.quickFailed 17494L<Cent> ValueNone; ActualPayment.quickFailed 17494L<Cent> ValueNone |]
                    42<OffsetDay>, [| ActualPayment.quickFailed 17494L<Cent> ValueNone |]
                    44<OffsetDay>, [| ActualPayment.quickFailed 17494L<Cent> ValueNone; ActualPayment.quickFailed 17494L<Cent> ValueNone |]
                    45<OffsetDay>, [| ActualPayment.quickFailed 17494L<Cent> ValueNone |]
                    47<OffsetDay>, [| ActualPayment.quickFailed 17494L<Cent> ValueNone; ActualPayment.quickFailed 17494L<Cent> ValueNone |]
                    48<OffsetDay>, [| ActualPayment.quickFailed 17494L<Cent> ValueNone |]
                    55<OffsetDay>, [| ActualPayment.quickFailed 17494L<Cent> ValueNone; ActualPayment.quickConfirmed 17494L<Cent> |]
                    56<OffsetDay>, [| ActualPayment.quickFailed 17494L<Cent> ValueNone |]
                    58<OffsetDay>, [| ActualPayment.quickFailed 17494L<Cent> ValueNone; ActualPayment.quickFailed 17494L<Cent> ValueNone |]
                    59<OffsetDay>, [| ActualPayment.quickConfirmed 17494L<Cent> |]
                    69<OffsetDay>, [| ActualPayment.quickFailed 17494L<Cent> ValueNone; ActualPayment.quickFailed 17494L<Cent> ValueNone |]
                    70<OffsetDay>, [| ActualPayment.quickFailed 17494L<Cent> ValueNone |]
                    72<OffsetDay>, [| ActualPayment.quickConfirmed 17494L<Cent> |]
                    83<OffsetDay>, [| ActualPayment.quickFailed 17494L<Cent> ValueNone; ActualPayment.quickFailed 17494L<Cent> ValueNone |]
                    84<OffsetDay>, [| ActualPayment.quickFailed 17494L<Cent> ValueNone |]
                    86<OffsetDay>, [| ActualPayment.quickConfirmed 17620L<Cent> |]
                    97<OffsetDay>, [| ActualPayment.quickConfirmed 17494L<Cent> |]
                    111<OffsetDay>, [| ActualPayment.quickFailed 17494L<Cent> ValueNone |]
                    112<OffsetDay>, [| ActualPayment.quickFailed 17494L<Cent> ValueNone |]
                    114<OffsetDay>, [| ActualPayment.quickConfirmed 17721L<Cent> |]
                    125<OffsetDay>, [| ActualPayment.quickFailed 17494L<Cent> ValueNone |]
                    126<OffsetDay>, [| ActualPayment.quickFailed 17494L<Cent> ValueNone |]
                    128<OffsetDay>, [| ActualPayment.quickConfirmed 17721L<Cent> |]
                    132<OffsetDay>, [| ActualPayment.quickConfirmed 19995L<Cent> |]
                ]

            let actual =
                let quote = getQuote SettlementDay.SettlementOnAsOfDay sp actualPayments
                quote.RevisedSchedules |> Schedule.outputHtmlToFile title description sp
                quote.RevisedSchedules.AmortisationSchedule.ScheduleStats.FinalApr

            let expected = Percent 516.758m

            actual |> should equal expected

        [<Fact>]
        let UnitPeriodConfigTest003 () =
            let title = "UnitPeriodConfigTest003"
            let description = "Irregular payment schedule does not break APR calculation"
            let sp = {
                AsOfDate = Date(2024, 3, 5)
                StartDate = Date(2022, 10, 13)
                Principal = 50000L<Cent>
                ScheduleConfig = AutoGenerateSchedule {
                    UnitPeriodConfig = Weekly(2, Date(2022, 10, 28))
                    PaymentCount = 11;
                    MaxDuration = Duration.Unlimited
                }
                PaymentConfig = {
                    LevelPaymentOption = LowerFinalPayment
                    ScheduledPaymentOption = AsScheduled
                    CloseBalanceOption = LeaveOpenBalance
                    PaymentRounding = RoundUp
                    MinimumPayment = DeferOrWriteOff 50L<Cent>
                    PaymentTimeout = 3<DurationDay>
                }
                FeeConfig = Some {
                    FeeTypes = [| Fee.FeeType.CabOrCsoFee (Amount.Percentage (Percent 154.47m, Restriction.NoLimit)) |]
                    Rounding = RoundDown
                    FeeAmortisation = Fee.FeeAmortisation.AmortiseProportionately
                    SettlementRefund = Fee.SettlementRefund.ProRata
                }
                ChargeConfig = None
                InterestConfig = {
                    Method = Interest.Method.Simple
                    StandardRate = Interest.Rate.Annual (Percent 9.95m)
                    Cap = Interest.Cap.Zero
                    InitialGracePeriod = 3<DurationDay>
                    PromotionalRates = [||]
                    RateOnNegativeBalance = Interest.Rate.Zero
                    AprMethod = Apr.CalculationMethod.UsActuarial 5
                    Rounding = RoundDown
                }
            }

            let actualPayments =
                Map [
                    12<OffsetDay>, [| ActualPayment.quickConfirmed 13465L<Cent> |]
                    26<OffsetDay>, [| ActualPayment.quickConfirmed 13465L<Cent> |]
                    41<OffsetDay>, [| ActualPayment.quickConfirmed 13465L<Cent> |]
                    54<OffsetDay>, [| ActualPayment.quickConfirmed 13465L<Cent> |]
                    73<OffsetDay>, [| ActualPayment.quickConfirmed 13465L<Cent> |]
                    88<OffsetDay>, [| ActualPayment.quickConfirmed 13562L<Cent> |]
                    101<OffsetDay>, [| ActualPayment.quickConfirmed 13580L<Cent> |]
                    117<OffsetDay>, [| ActualPayment.quickConfirmed 13695L<Cent> |]
                    127<OffsetDay>, [| ActualPayment.quickConfirmed 13465L<Cent> |]
                    134<OffsetDay>, [| ActualPayment.quickConfirmed 15560L<Cent> |]
                ]

            let actual =
                let quote = getQuote SettlementDay.SettlementOnAsOfDay sp actualPayments
                quote.RevisedSchedules |> Schedule.outputHtmlToFile title description sp
                quote.RevisedSchedules.AmortisationSchedule.ScheduleStats.FinalApr

            let expected = Percent 930.559m

            actual |> should equal expected

        [<Fact>]
        let UnitPeriodConfigTest004 () =
            let title = "UnitPeriodConfigTest004"
            let description = "Checking that the fees refund behaves correctly"
            let startDate = Date(2023, 1, 16)
            let originalScheduledPayments =
                Map [
                    4<OffsetDay>, ScheduledPayment.quick (ValueSome 11859L<Cent>) ValueNone
                    11<OffsetDay>, ScheduledPayment.quick (ValueSome 11859L<Cent>) ValueNone
                    18<OffsetDay>, ScheduledPayment.quick (ValueSome 11859L<Cent>) ValueNone
                    25<OffsetDay>, ScheduledPayment.quick (ValueSome 11859L<Cent>) ValueNone
                    32<OffsetDay>, ScheduledPayment.quick (ValueSome 11859L<Cent>) ValueNone
                    39<OffsetDay>, ScheduledPayment.quick (ValueSome 11859L<Cent>) ValueNone
                    46<OffsetDay>, ScheduledPayment.quick (ValueSome 11859L<Cent>) ValueNone
                    53<OffsetDay>, ScheduledPayment.quick (ValueSome 11859L<Cent>) ValueNone
                    60<OffsetDay>, ScheduledPayment.quick (ValueSome 11859L<Cent>) ValueNone
                    67<OffsetDay>, ScheduledPayment.quick (ValueSome 11859L<Cent>) ValueNone
                    74<OffsetDay>, ScheduledPayment.quick (ValueSome 11859L<Cent>) ValueNone
                    81<OffsetDay>, ScheduledPayment.quick (ValueSome 11859L<Cent>) ValueNone
                    88<OffsetDay>, ScheduledPayment.quick (ValueSome 11859L<Cent>) ValueNone
                    95<OffsetDay>, ScheduledPayment.quick (ValueSome 11859L<Cent>) ValueNone
                    102<OffsetDay>, ScheduledPayment.quick (ValueSome 11859L<Cent>) ValueNone
                    109<OffsetDay>, ScheduledPayment.quick (ValueSome 11859L<Cent>) ValueNone
                    116<OffsetDay>, ScheduledPayment.quick (ValueSome 11859L<Cent>) ValueNone
                    123<OffsetDay>, ScheduledPayment.quick (ValueSome 11859L<Cent>) ValueNone
                    130<OffsetDay>, ScheduledPayment.quick (ValueSome 11859L<Cent>) ValueNone
                    137<OffsetDay>, ScheduledPayment.quick (ValueSome 11859L<Cent>) ValueNone
                    144<OffsetDay>, ScheduledPayment.quick (ValueSome 11859L<Cent>) ValueNone
                    151<OffsetDay>, ScheduledPayment.quick (ValueSome 11859L<Cent>) ValueNone
                    158<OffsetDay>, ScheduledPayment.quick (ValueSome 11859L<Cent>) ValueNone
                    165<OffsetDay>, ScheduledPayment.quick (ValueSome 11859L<Cent>) ValueNone
                    172<OffsetDay>, ScheduledPayment.quick (ValueSome 11846L<Cent>) ValueNone
                ]

            let sp = {
                AsOfDate = startDate
                StartDate = startDate
                Principal = 100000L<Cent>
                ScheduleConfig = CustomSchedule originalScheduledPayments
                PaymentConfig = {
                    LevelPaymentOption = LowerFinalPayment
                    ScheduledPaymentOption = AsScheduled
                    CloseBalanceOption = LeaveOpenBalance
                    PaymentRounding = RoundUp
                    MinimumPayment = DeferOrWriteOff 50L<Cent>
                    PaymentTimeout = 3<DurationDay>
                }
                FeeConfig = Some {
                    FeeTypes = [| Fee.FeeType.CabOrCsoFee (Amount.Percentage (Percent 189.47m, Restriction.NoLimit)) |]
                    Rounding = RoundDown
                    FeeAmortisation = Fee.FeeAmortisation.AmortiseProportionately
                    SettlementRefund = Fee.SettlementRefund.ProRata
                }
                ChargeConfig = None
                InterestConfig = {
                    Method = Interest.Method.Simple
                    StandardRate = Interest.Rate.Annual (Percent 9.95m)
                    Cap = Interest.Cap.Zero
                    InitialGracePeriod = 3<DurationDay>
                    PromotionalRates = [||]
                    RateOnNegativeBalance = Interest.Rate.Zero
                    AprMethod = Apr.CalculationMethod.UsActuarial 5
                    Rounding = RoundDown
                }
            }

            let actualPayments =
                [|
                    13<OffsetDay>, [| ActualPayment.quickConfirmed 11859L<Cent> |]
                    18<OffsetDay>, [| ActualPayment.quickConfirmed 11859L<Cent> |]
                    74<OffsetDay>, [| ActualPayment.quickConfirmed 11859L<Cent> |]
                    75<OffsetDay>, [| ActualPayment.quickConfirmed 11859L<Cent> |]
                    81<OffsetDay>, [| ActualPayment.quickConfirmed 11859L<Cent> |]
                    81<OffsetDay>, [| ActualPayment.quickConfirmed 11859L<Cent> |]
                    81<OffsetDay>, [| ActualPayment.quickConfirmed 11859L<Cent> |]
                    82<OffsetDay>, [| ActualPayment.quickConfirmed 11859L<Cent> |]
                    87<OffsetDay>, [| ActualPayment.quickConfirmed 11859L<Cent> |]
                    87<OffsetDay>, [| ActualPayment.quickConfirmed 11859L<Cent> |]
                    88<OffsetDay>, [| ActualPayment.quickConfirmed 11859L<Cent> |]
                    95<OffsetDay>, [| ActualPayment.quickConfirmed 12363L<Cent> |]
                    98<OffsetDay>, [| ActualPayment.quickConfirmed 12096L<Cent> |]
                    201<OffsetDay>, [| ActualPayment.quickConfirmed 12489L<Cent> |]
                    234<OffsetDay>, [| ActualPayment.quickConfirmed 12489L<Cent> |]
                |]
                |> Map.ofArrayWithMerge

            let actual =
                let originalFinalPaymentDay = originalScheduledPayments |> Map.maxKeyValue |> fst
                let quoteSp =
                    { sp with
                        AsOfDate = Date(2024, 3, 6)
                        ScheduleConfig =
                            [|
                                Map.toArray originalScheduledPayments
                                [|
                                    201<OffsetDay>, ScheduledPayment.quick ValueNone (ValueSome { Value = 12489L<Cent>; RescheduleDay = 198<OffsetDay> })
                                    232<OffsetDay>, ScheduledPayment.quick ValueNone (ValueSome { Value = 12489L<Cent>; RescheduleDay = 198<OffsetDay> })
                                    262<OffsetDay>, ScheduledPayment.quick ValueNone (ValueSome { Value = 12489L<Cent>; RescheduleDay = 198<OffsetDay> })
                                    293<OffsetDay>, ScheduledPayment.quick ValueNone (ValueSome { Value = 12489L<Cent>; RescheduleDay = 198<OffsetDay> })
                                    323<OffsetDay>, ScheduledPayment.quick ValueNone (ValueSome { Value = 12489L<Cent>; RescheduleDay = 198<OffsetDay> })
                                    354<OffsetDay>, ScheduledPayment.quick ValueNone (ValueSome { Value = 79109L<Cent>; RescheduleDay = 198<OffsetDay> })
                                |]
                            |]
                            |> Array.concat
                            |> Map.ofArray
                            |> CustomSchedule
                        FeeConfig =
                            sp.FeeConfig
                            |> Option.map(fun fc ->
                                { fc with
                                    SettlementRefund =
                                        match fc.SettlementRefund with
                                        | Fee.SettlementRefund.ProRata
                                        | Fee.SettlementRefund.ProRataRescheduled _ ->
                                            Fee.SettlementRefund.ProRataRescheduled originalFinalPaymentDay
                                        | _ as fsr ->
                                            fsr
                                }
                            )
                    }
                let quote = getQuote SettlementDay.SettlementOnAsOfDay quoteSp actualPayments
                quote.RevisedSchedules |> Schedule.outputHtmlToFile title description sp
                quote.RevisedSchedules.AmortisationSchedule.ScheduleStats.FinalApr

            let expected = Percent 699.525m

            actual |> should equal expected
