namespace FSharp.Finance.Personal.Tests

open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal

module UnitPeriodConfigTests =

    open ArrayExtension
    open Amortisation
    open Calculation
    open Currency
    open CustomerPayments
    open FeesAndCharges
    open PaymentSchedule
    open Percentages
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

        let finalAprPercent = function
        | ValueSome (Solution.Found _, ValueSome percent) -> percent
        | _ -> Percent 0m

        [<Fact>]
        let ``1) Irregular payment schedule does not break detect function`` () =
            let sp = {
                    AsOfDate = Date(2024, 3, 5)
                    ScheduleType = ScheduleType.Original
                    StartDate = Date(2022, 5, 5)
                    Principal = 100000L<Cent>
                    PaymentSchedule = RegularSchedule(Weekly(2, Date(2022, 5, 13)), 12)
                    FeesAndCharges = {
                        Fees = [| Fee.CabOrCsoFee (Amount.Percentage (Percent 154.47m, ValueNone, ValueSome RoundDown)) |]
                        FeesSettlement = Fees.SettlementRefund.ProRata
                        Charges = [||]
                        ChargesHolidays = [||]
                        ChargesGrouping = OneChargeTypePerDay
                        LatePaymentGracePeriod = 3<DurationDay>
                    }
                    Interest = {
                        Rate = Interest.Rate.Annual (Percent 9.95m)
                        Cap = Interest.Cap.none
                        InitialGracePeriod = 3<DurationDay>
                        Holidays = [||]
                        RateOnNegativeBalance = ValueNone
                    }
                    Calculation = {
                        AprMethod = Apr.CalculationMethod.UsActuarial 5
                        RoundingOptions = RoundingOptions.recommended
                        MinimumPayment = DeferOrWriteOff 50L<Cent>
                        PaymentTimeout = 3<DurationDay>
                        NegativeInterestOption = ApplyNegativeInterest
                    }
                }
            
            let actualPayments = [|
                { PaymentDay = 8<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 21700L<Cent>) }
                { PaymentDay = 22<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (21700L<Cent>, [||])) }
                { PaymentDay = 22<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (21700L<Cent>, [||])) }
                { PaymentDay = 22<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (21700L<Cent>, [||])) }
                { PaymentDay = 25<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 21700L<Cent>) }
                { PaymentDay = 39<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (21700L<Cent>, [||])) }
                { PaymentDay = 39<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (21700L<Cent>, [||])) }
                { PaymentDay = 39<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (21700L<Cent>, [||])) }
                { PaymentDay = 45<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (21700L<Cent>, [||])) }
                { PaymentDay = 45<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 21700L<Cent>) }
                { PaymentDay = 50<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (21700L<Cent>, [||])) }
                { PaymentDay = 50<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (21700L<Cent>, [||])) }
                { PaymentDay = 50<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (21700L<Cent>, [||])) }
                { PaymentDay = 53<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (21700L<Cent>, [||])) }
                { PaymentDay = 53<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (21700L<Cent>, [||])) }
                { PaymentDay = 53<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (21700L<Cent>, [||])) }
                { PaymentDay = 56<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 21700L<Cent>) }
                { PaymentDay = 67<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (21700L<Cent>, [||])) }
                { PaymentDay = 67<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (21700L<Cent>, [||])) }
                { PaymentDay = 67<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (21700L<Cent>, [||])) }
                { PaymentDay = 73<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (21700L<Cent>, [||])) }
                { PaymentDay = 73<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (21700L<Cent>, [||])) }
                { PaymentDay = 73<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (21700L<Cent>, [||])) }
                { PaymentDay = 78<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (21700L<Cent>, [||])) }
                { PaymentDay = 78<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (21700L<Cent>, [||])) }
                { PaymentDay = 78<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (21700L<Cent>, [||])) }
                { PaymentDay = 79<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (21700L<Cent>, [||])) }
                { PaymentDay = 79<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (21700L<Cent>, [||])) }
                { PaymentDay = 79<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (21700L<Cent>, [||])) }
                { PaymentDay = 274<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (26036L<Cent>, [||])) }
                { PaymentDay = 302<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (26036L<Cent>, [||])) }
                { PaymentDay = 330<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 21700L<Cent>) }
            |]

            let actual =
                voption {
                    let! quote = getQuote Settlement sp actualPayments
                    quote.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/UnitPeriodConfigTest001.md"
                    return quote.RevisedSchedule.FinalApr |> finalAprPercent
                }

            let expected = ValueSome (Percent 56.51300m)
            actual |> should equal expected

        [<Fact>]
        let ``2) Irregular payment schedule does not break APR calculation`` () =
            let sp = {
                AsOfDate = Date(2024, 3, 5)
                ScheduleType = ScheduleType.Original
                StartDate = Date(2023, 4, 13)
                Principal = 70000L<Cent>
                PaymentSchedule = RegularSchedule(Weekly(2, Date(2023, 4, 20)), 12)
                FeesAndCharges = {
                    Fees = [| Fee.CabOrCsoFee (Amount.Percentage (Percent 154.47m, ValueNone, ValueSome RoundDown)) |]
                    FeesSettlement = Fees.SettlementRefund.ProRata
                    Charges = [||]
                    ChargesHolidays = [||]
                    ChargesGrouping = OneChargeTypePerDay
                    LatePaymentGracePeriod = 3<DurationDay>
                }
                Interest = {
                    Rate = Interest.Rate.Annual (Percent 9.95m)
                    Cap = Interest.Cap.none
                    InitialGracePeriod = 3<DurationDay>
                    Holidays = [||]
                    RateOnNegativeBalance = ValueNone
                }
                Calculation = {
                    AprMethod = Apr.CalculationMethod.UsActuarial 5
                    RoundingOptions = RoundingOptions.recommended
                    MinimumPayment = DeferOrWriteOff 50L<Cent>
                    PaymentTimeout = 3<DurationDay>
                    NegativeInterestOption = ApplyNegativeInterest
                }
            }

            let actualPayments = [|
                { PaymentDay = 6<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 17275L<Cent>) }
                { PaymentDay = 21<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 17275L<Cent>) }
                { PaymentDay = 35<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 17275L<Cent>) }
                { PaymentDay = 49<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (17275L<Cent>, [||])) }
                { PaymentDay = 49<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (17275L<Cent>, [||])) }
                { PaymentDay = 50<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 17275L<Cent>) }
                { PaymentDay = 64<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 17275L<Cent>) }
                { PaymentDay = 80<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 17275L<Cent>) }
                { PaymentDay = 94<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 17488L<Cent>) }
                { PaymentDay = 108<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (17488L<Cent>, [||])) }
                { PaymentDay = 108<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (17488L<Cent>, [||])) }
                { PaymentDay = 109<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (17488L<Cent>, [||])) }
                { PaymentDay = 111<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (17701L<Cent>, [||])) }
                { PaymentDay = 111<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (17701L<Cent>, [||])) }
                { PaymentDay = 112<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (17772L<Cent>, [||])) }
                { PaymentDay = 112<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (17772L<Cent>, [||])) }
                { PaymentDay = 112<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 17772L<Cent>) }
                { PaymentDay = 122<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 17488L<Cent>) }
                { PaymentDay = 128<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 23521L<Cent>) }
            |]

            let actual =
                voption {
                    let! quote = getQuote Settlement sp actualPayments
                    quote.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/UnitPeriodConfigTest002.md"
                    return quote.RevisedSchedule.FinalApr |> finalAprPercent
                }

            let expected = ValueSome (Percent 986.81300m)
            actual |> should equal expected

        [<Fact>]
        let ``3) Irregular payment schedule does not break APR calculation`` () =
            let sp = {
                AsOfDate = Date(2024, 3, 5)
                ScheduleType = ScheduleType.Original
                StartDate = Date(2023, 1, 20)
                Principal = 65000L<Cent>
                PaymentSchedule = RegularSchedule(Weekly(2, Date(2023, 2, 2)), 11)
                FeesAndCharges = {
                    Fees = [| Fee.CabOrCsoFee (Amount.Percentage (Percent 154.47m, ValueNone, ValueSome RoundDown)) |]
                    FeesSettlement = Fees.SettlementRefund.ProRata
                    Charges = [||]
                    ChargesHolidays = [||]
                    ChargesGrouping = OneChargeTypePerDay
                    LatePaymentGracePeriod = 3<DurationDay>
                }
                Interest = {
                    Rate = Interest.Rate.Annual (Percent 9.95m)
                    Cap = Interest.Cap.none
                    InitialGracePeriod = 3<DurationDay>
                    Holidays = [||]
                    RateOnNegativeBalance = ValueNone
                }
                Calculation = {
                    AprMethod = Apr.CalculationMethod.UsActuarial 5
                    RoundingOptions = RoundingOptions.recommended
                    MinimumPayment = DeferOrWriteOff 50L<Cent>
                    PaymentTimeout = 3<DurationDay>
                    NegativeInterestOption = ApplyNegativeInterest
                }
            }

            let actualPayments = [|
                { PaymentDay = 13<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (17494L<Cent>, [||])) }
                { PaymentDay = 13<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (17494L<Cent>, [||])) }
                { PaymentDay = 14<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (17494L<Cent>, [||])) }
                { PaymentDay = 16<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (17494L<Cent>, [||])) }
                { PaymentDay = 16<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (17494L<Cent>, [||])) }
                { PaymentDay = 17<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (17494L<Cent>, [||])) }
                { PaymentDay = 19<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 17494L<Cent>) }
                { PaymentDay = 27<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (17494L<Cent>, [||])) }
                { PaymentDay = 27<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (17494L<Cent>, [||])) }
                { PaymentDay = 28<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (17494L<Cent>, [||])) }
                { PaymentDay = 30<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (17494L<Cent>, [||])) }
                { PaymentDay = 30<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (17494L<Cent>, [||])) }
                { PaymentDay = 31<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (17494L<Cent>, [||])) }
                { PaymentDay = 33<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (17494L<Cent>, [||])) }
                { PaymentDay = 33<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (17494L<Cent>, [||])) }
                { PaymentDay = 34<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (17494L<Cent>, [||])) }
                { PaymentDay = 36<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 17494L<Cent>) }
                { PaymentDay = 41<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (17494L<Cent>, [||])) }
                { PaymentDay = 41<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (17494L<Cent>, [||])) }
                { PaymentDay = 42<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (17494L<Cent>, [||])) }
                { PaymentDay = 44<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (17494L<Cent>, [||])) }
                { PaymentDay = 44<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (17494L<Cent>, [||])) }
                { PaymentDay = 45<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (17494L<Cent>, [||])) }
                { PaymentDay = 47<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (17494L<Cent>, [||])) }
                { PaymentDay = 47<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (17494L<Cent>, [||])) }
                { PaymentDay = 48<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (17494L<Cent>, [||])) }
                { PaymentDay = 55<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (17494L<Cent>, [||])) }
                { PaymentDay = 55<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 17494L<Cent>) }
                { PaymentDay = 56<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (17494L<Cent>, [||])) }
                { PaymentDay = 58<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (17494L<Cent>, [||])) }
                { PaymentDay = 58<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (17494L<Cent>, [||])) }
                { PaymentDay = 59<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 17494L<Cent>) }
                { PaymentDay = 69<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (17494L<Cent>, [||])) }
                { PaymentDay = 69<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (17494L<Cent>, [||])) }
                { PaymentDay = 70<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (17494L<Cent>, [||])) }
                { PaymentDay = 72<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 17494L<Cent>) }
                { PaymentDay = 83<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (17494L<Cent>, [||])) }
                { PaymentDay = 83<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (17494L<Cent>, [||])) }
                { PaymentDay = 84<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (17494L<Cent>, [||])) }
                { PaymentDay = 86<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 17620L<Cent>) }
                { PaymentDay = 97<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 17494L<Cent>) }
                { PaymentDay = 111<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (17494L<Cent>, [||])) }
                { PaymentDay = 112<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (17494L<Cent>, [||])) }
                { PaymentDay = 114<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 17721L<Cent>) }
                { PaymentDay = 125<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (17494L<Cent>, [||])) }
                { PaymentDay = 126<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (17494L<Cent>, [||])) }
                { PaymentDay = 128<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 17721L<Cent>) }
                { PaymentDay = 132<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 19995L<Cent>) }
            |]

            let actual =
                voption {
                    let! quote = getQuote Settlement sp actualPayments
                    quote.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/UnitPeriodConfigTest003.md"
                    return quote.RevisedSchedule.FinalApr |> finalAprPercent
                }

            let expected = ValueSome (Percent 516.75800m)
            actual |> should equal expected

        [<Fact>]
        let ``4) Irregular payment schedule does not break APR calculation`` () =
            let sp = {
                AsOfDate = Date(2024, 3, 5)
                ScheduleType = ScheduleType.Original
                StartDate = Date(2022, 10, 13)
                Principal = 50000L<Cent>
                PaymentSchedule = RegularSchedule(Weekly(2, Date(2022, 10, 28)), 11)
                FeesAndCharges = {
                    Fees = [| Fee.CabOrCsoFee (Amount.Percentage (Percent 154.47m, ValueNone, ValueSome RoundDown)) |]
                    FeesSettlement = Fees.SettlementRefund.ProRata
                    Charges = [||]
                    ChargesHolidays = [||]
                    ChargesGrouping = OneChargeTypePerDay
                    LatePaymentGracePeriod = 3<DurationDay>
                }
                Interest = {
                    Rate = Interest.Rate.Annual (Percent 9.95m)
                    Cap = Interest.Cap.none
                    InitialGracePeriod = 3<DurationDay>
                    Holidays = [||]
                    RateOnNegativeBalance = ValueNone
                }
                Calculation = {
                    AprMethod = Apr.CalculationMethod.UsActuarial 5
                    RoundingOptions = RoundingOptions.recommended
                    MinimumPayment = DeferOrWriteOff 50L<Cent>
                    PaymentTimeout = 3<DurationDay>
                    NegativeInterestOption = ApplyNegativeInterest
                }
            }

            let actualPayments = [|
                { PaymentDay = 12<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 13465L<Cent>) }
                { PaymentDay = 26<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 13465L<Cent>) }
                { PaymentDay = 41<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 13465L<Cent>) }
                { PaymentDay = 54<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 13465L<Cent>) }
                { PaymentDay = 73<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 13465L<Cent>) }
                { PaymentDay = 88<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 13562L<Cent>) }
                { PaymentDay = 101<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 13580L<Cent>) }
                { PaymentDay = 117<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 13695L<Cent>) }
                { PaymentDay = 127<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 13465L<Cent>) }
                { PaymentDay = 134<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 15560L<Cent>) }
            |]

            let actual =
                voption {
                    let! quote = getQuote Settlement sp actualPayments
                    quote.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/UnitPeriodConfigTest004.md"
                    return quote.RevisedSchedule.FinalApr |> finalAprPercent
                }

            let expected = ValueSome (Percent 930.55900m)
            actual |> should equal expected

        [<Fact>]
        let ``5) Checking that the fees refund behaves correctly`` () =
            let startDate = Date(2023, 1, 16)
            let originalScheduledPayments = [|
                { PaymentDay = 4<OffsetDay>; PaymentDetails = ScheduledPayment (ScheduledPaymentType.Original 11859L<Cent>) }
                { PaymentDay = 11<OffsetDay>; PaymentDetails = ScheduledPayment (ScheduledPaymentType.Original 11859L<Cent>) }
                { PaymentDay = 18<OffsetDay>; PaymentDetails = ScheduledPayment (ScheduledPaymentType.Original 11859L<Cent>) }
                { PaymentDay = 25<OffsetDay>; PaymentDetails = ScheduledPayment (ScheduledPaymentType.Original 11859L<Cent>) }
                { PaymentDay = 32<OffsetDay>; PaymentDetails = ScheduledPayment (ScheduledPaymentType.Original 11859L<Cent>) }
                { PaymentDay = 39<OffsetDay>; PaymentDetails = ScheduledPayment (ScheduledPaymentType.Original 11859L<Cent>) }
                { PaymentDay = 46<OffsetDay>; PaymentDetails = ScheduledPayment (ScheduledPaymentType.Original 11859L<Cent>) }
                { PaymentDay = 53<OffsetDay>; PaymentDetails = ScheduledPayment (ScheduledPaymentType.Original 11859L<Cent>) }
                { PaymentDay = 60<OffsetDay>; PaymentDetails = ScheduledPayment (ScheduledPaymentType.Original 11859L<Cent>) }
                { PaymentDay = 67<OffsetDay>; PaymentDetails = ScheduledPayment (ScheduledPaymentType.Original 11859L<Cent>) }
                { PaymentDay = 74<OffsetDay>; PaymentDetails = ScheduledPayment (ScheduledPaymentType.Original 11859L<Cent>) }
                { PaymentDay = 81<OffsetDay>; PaymentDetails = ScheduledPayment (ScheduledPaymentType.Original 11859L<Cent>) }
                { PaymentDay = 88<OffsetDay>; PaymentDetails = ScheduledPayment (ScheduledPaymentType.Original 11859L<Cent>) }
                { PaymentDay = 95<OffsetDay>; PaymentDetails = ScheduledPayment (ScheduledPaymentType.Original 11859L<Cent>) }
                { PaymentDay = 102<OffsetDay>; PaymentDetails = ScheduledPayment (ScheduledPaymentType.Original 11859L<Cent>) }
                { PaymentDay = 109<OffsetDay>; PaymentDetails = ScheduledPayment (ScheduledPaymentType.Original 11859L<Cent>) }
                { PaymentDay = 116<OffsetDay>; PaymentDetails = ScheduledPayment (ScheduledPaymentType.Original 11859L<Cent>) }
                { PaymentDay = 123<OffsetDay>; PaymentDetails = ScheduledPayment (ScheduledPaymentType.Original 11859L<Cent>) }
                { PaymentDay = 130<OffsetDay>; PaymentDetails = ScheduledPayment (ScheduledPaymentType.Original 11859L<Cent>) }
                { PaymentDay = 137<OffsetDay>; PaymentDetails = ScheduledPayment (ScheduledPaymentType.Original 11859L<Cent>) }
                { PaymentDay = 144<OffsetDay>; PaymentDetails = ScheduledPayment (ScheduledPaymentType.Original 11859L<Cent>) }
                { PaymentDay = 151<OffsetDay>; PaymentDetails = ScheduledPayment (ScheduledPaymentType.Original 11859L<Cent>) }
                { PaymentDay = 158<OffsetDay>; PaymentDetails = ScheduledPayment (ScheduledPaymentType.Original 11859L<Cent>) }
                { PaymentDay = 165<OffsetDay>; PaymentDetails = ScheduledPayment (ScheduledPaymentType.Original 11859L<Cent>) }
                { PaymentDay = 172<OffsetDay>; PaymentDetails = ScheduledPayment (ScheduledPaymentType.Original 11846L<Cent>) }
            |]

            let sp = {
                AsOfDate = startDate
                ScheduleType = ScheduleType.Original
                StartDate = startDate
                Principal = 100000L<Cent>
                PaymentSchedule = IrregularSchedule originalScheduledPayments
                FeesAndCharges = {
                    Fees = [| Fee.CabOrCsoFee (Amount.Percentage (Percent 189.47m, ValueNone, ValueSome RoundDown)) |]
                    FeesSettlement = Fees.SettlementRefund.ProRata
                    Charges = [||]
                    ChargesHolidays = [||]
                    ChargesGrouping = OneChargeTypePerDay
                    LatePaymentGracePeriod = 3<DurationDay>
                }
                Interest = {
                    Rate = Interest.Rate.Annual (Percent 9.95m)
                    Cap = Interest.Cap.none
                    InitialGracePeriod = 3<DurationDay>
                    Holidays = [||]
                    RateOnNegativeBalance = ValueNone
                }
                Calculation = {
                    AprMethod = Apr.CalculationMethod.UsActuarial 5
                    RoundingOptions = RoundingOptions.recommended
                    MinimumPayment = DeferOrWriteOff 50L<Cent>
                    PaymentTimeout = 3<DurationDay>
                    NegativeInterestOption = ApplyNegativeInterest
                }
            }

            let actualPayments = [|
                // { PaymentDay = 4<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 4<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 5<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 5<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 6<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 8<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 8<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 9<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 11<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 11<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 12<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 12<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 12<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                { PaymentDay = 13<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 11859L<Cent>) }
                // { PaymentDay = 14<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 14<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 15<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 18<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                { PaymentDay = 18<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 11859L<Cent>) }
                // { PaymentDay = 19<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 21<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 21<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 22<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 24<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 24<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 25<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 25<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 26<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 28<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 28<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 29<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 31<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 31<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 32<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 32<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 32<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 33<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 35<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 35<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 36<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 38<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 38<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 39<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 39<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 39<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 40<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 42<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 42<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 43<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 45<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 45<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 46<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 46<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 46<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 47<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 49<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 49<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 50<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 52<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 52<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 53<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 53<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 53<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 54<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 56<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 56<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 57<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 59<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 59<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 60<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 60<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 60<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 61<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 63<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 63<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 64<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 66<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 66<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 67<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 67<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 67<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 68<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 70<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 70<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 71<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 73<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 73<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 74<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                { PaymentDay = 74<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 11859L<Cent>) }
                // { PaymentDay = 74<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                { PaymentDay = 75<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 11859L<Cent>) }
                // { PaymentDay = 77<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 77<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 78<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 80<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 80<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                { PaymentDay = 81<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 11859L<Cent>) }
                { PaymentDay = 81<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 11859L<Cent>) }
                { PaymentDay = 81<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 11859L<Cent>) }
                { PaymentDay = 82<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 11859L<Cent>) }
                // { PaymentDay = 84<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 84<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 85<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                { PaymentDay = 87<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 11859L<Cent>) }
                { PaymentDay = 87<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 11859L<Cent>) }
                { PaymentDay = 88<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 11859L<Cent>) }
                // { PaymentDay = 88<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 88<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 89<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 91<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (12048L<Cent>, [||])) }
                // { PaymentDay = 91<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (12048L<Cent>, [||])) }
                // { PaymentDay = 92<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (12048L<Cent>, [||])) }
                // { PaymentDay = 94<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (12285L<Cent>, [||])) }
                // { PaymentDay = 94<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (12285L<Cent>, [||])) }
                // { PaymentDay = 95<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (12285L<Cent>, [||])) }
                { PaymentDay = 95<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 12363L<Cent>) }
                // { PaymentDay = 95<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 96<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 98<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (12096L<Cent>, [||])) }
                { PaymentDay = 98<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 12096L<Cent>) }
                // { PaymentDay = 99<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (11859L<Cent>, [||])) }
                // { PaymentDay = 101<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (12096L<Cent>, [||])) }
                // { PaymentDay = 101<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (12096L<Cent>, [||])) }
                // { PaymentDay = 102<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (12096L<Cent>, [||])) }
                // { PaymentDay = 102<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (12175L<Cent>, [||])) }
                // { PaymentDay = 102<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (12175L<Cent>, [||])) }
                // { PaymentDay = 103<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (12175L<Cent>, [||])) }
                // { PaymentDay = 105<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (12316L<Cent>, [||])) }
                // { PaymentDay = 105<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (12316L<Cent>, [||])) }
                // { PaymentDay = 106<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (12316L<Cent>, [||])) }
                // { PaymentDay = 108<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (12453L<Cent>, [||])) }
                // { PaymentDay = 108<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (12453L<Cent>, [||])) }
                // { PaymentDay = 109<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (12453L<Cent>, [||])) }
                // { PaymentDay = 109<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (12499L<Cent>, [||])) }
                // { PaymentDay = 109<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (12499L<Cent>, [||])) }
                // { PaymentDay = 110<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (12499L<Cent>, [||])) }
                // { PaymentDay = 112<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (12636L<Cent>, [||])) }
                // { PaymentDay = 112<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (12636L<Cent>, [||])) }
                // { PaymentDay = 113<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (12636L<Cent>, [||])) }
                // { PaymentDay = 115<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (12773L<Cent>, [||])) }
                // { PaymentDay = 116<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (12773L<Cent>, [||])) }
                // { PaymentDay = 116<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (12818L<Cent>, [||])) }
                // { PaymentDay = 116<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (12818L<Cent>, [||])) }
                // { PaymentDay = 117<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (12818L<Cent>, [||])) }
                // { PaymentDay = 119<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (12956L<Cent>, [||])) }
                // { PaymentDay = 119<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (12956L<Cent>, [||])) }
                // { PaymentDay = 120<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (12956L<Cent>, [||])) }
                // { PaymentDay = 122<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (13093L<Cent>, [||])) }
                // { PaymentDay = 123<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (13093L<Cent>, [||])) }
                // { PaymentDay = 123<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (13138L<Cent>, [||])) }
                // { PaymentDay = 123<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (13138L<Cent>, [||])) }
                // { PaymentDay = 124<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (13138L<Cent>, [||])) }
                // { PaymentDay = 126<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (13275L<Cent>, [||])) }
                // { PaymentDay = 126<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (13275L<Cent>, [||])) }
                // { PaymentDay = 127<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (13275L<Cent>, [||])) }
                // { PaymentDay = 129<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (13412L<Cent>, [||])) }
                // { PaymentDay = 129<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (13412L<Cent>, [||])) }
                // { PaymentDay = 130<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (13412L<Cent>, [||])) }
                // { PaymentDay = 130<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (13458L<Cent>, [||])) }
                // { PaymentDay = 130<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (13458L<Cent>, [||])) }
                // { PaymentDay = 131<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (13458L<Cent>, [||])) }
                // { PaymentDay = 133<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (13595L<Cent>, [||])) }
                // { PaymentDay = 133<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (13595L<Cent>, [||])) }
                // { PaymentDay = 134<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (13595L<Cent>, [||])) }
                // { PaymentDay = 136<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (13732L<Cent>, [||])) }
                // { PaymentDay = 136<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (13732L<Cent>, [||])) }
                // { PaymentDay = 137<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (13732L<Cent>, [||])) }
                // { PaymentDay = 137<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (13778L<Cent>, [||])) }
                // { PaymentDay = 137<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (13778L<Cent>, [||])) }
                // { PaymentDay = 138<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (13778L<Cent>, [||])) }
                // { PaymentDay = 140<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (13915L<Cent>, [||])) }
                // { PaymentDay = 140<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (13915L<Cent>, [||])) }
                // { PaymentDay = 141<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (13915L<Cent>, [||])) }
                // { PaymentDay = 143<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (14052L<Cent>, [||])) }
                // { PaymentDay = 143<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (14052L<Cent>, [||])) }
                // { PaymentDay = 144<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (14052L<Cent>, [||])) }
                // { PaymentDay = 144<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (14098L<Cent>, [||])) }
                // { PaymentDay = 144<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (14098L<Cent>, [||])) }
                // { PaymentDay = 145<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (14098L<Cent>, [||])) }
                // { PaymentDay = 147<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (14235L<Cent>, [||])) }
                // { PaymentDay = 147<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (14235L<Cent>, [||])) }
                // { PaymentDay = 148<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (14235L<Cent>, [||])) }
                // { PaymentDay = 150<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (14372L<Cent>, [||])) }
                // { PaymentDay = 150<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (14372L<Cent>, [||])) }
                // { PaymentDay = 151<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (14372L<Cent>, [||])) }
                // { PaymentDay = 151<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (14418L<Cent>, [||])) }
                // { PaymentDay = 151<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (14418L<Cent>, [||])) }
                // { PaymentDay = 152<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (14418L<Cent>, [||])) }
                // { PaymentDay = 154<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (14555L<Cent>, [||])) }
                // { PaymentDay = 154<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (14555L<Cent>, [||])) }
                // { PaymentDay = 155<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (14555L<Cent>, [||])) }
                // { PaymentDay = 157<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (14692L<Cent>, [||])) }
                // { PaymentDay = 157<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (14692L<Cent>, [||])) }
                // { PaymentDay = 158<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (14692L<Cent>, [||])) }
                // { PaymentDay = 158<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (14737L<Cent>, [||])) }
                // { PaymentDay = 158<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (14737L<Cent>, [||])) }
                // { PaymentDay = 159<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (14737L<Cent>, [||])) }
                // { PaymentDay = 161<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (14874L<Cent>, [||])) }
                // { PaymentDay = 161<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (14874L<Cent>, [||])) }
                // { PaymentDay = 162<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (14874L<Cent>, [||])) }
                // { PaymentDay = 164<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (15012L<Cent>, [||])) }
                // { PaymentDay = 164<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (15012L<Cent>, [||])) }
                // { PaymentDay = 165<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (15012L<Cent>, [||])) }
                // { PaymentDay = 165<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (15057L<Cent>, [||])) }
                // { PaymentDay = 165<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (15057L<Cent>, [||])) }
                // { PaymentDay = 166<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (15057L<Cent>, [||])) }
                // { PaymentDay = 168<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (15194L<Cent>, [||])) }
                // { PaymentDay = 168<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (15194L<Cent>, [||])) }
                // { PaymentDay = 169<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (15194L<Cent>, [||])) }
                // { PaymentDay = 171<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (15331L<Cent>, [||])) }
                // { PaymentDay = 171<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (15331L<Cent>, [||])) }
                // { PaymentDay = 172<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (15331L<Cent>, [||])) }
                // { PaymentDay = 172<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (15377L<Cent>, [||])) }
                // { PaymentDay = 172<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (15377L<Cent>, [||])) }
                // { PaymentDay = 173<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (15377L<Cent>, [||])) }
                // { PaymentDay = 175<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (15514L<Cent>, [||])) }
                // { PaymentDay = 175<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (15514L<Cent>, [||])) }
                // { PaymentDay = 176<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (15514L<Cent>, [||])) }
                // { PaymentDay = 178<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (15651L<Cent>, [||])) }
                // { PaymentDay = 178<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (15651L<Cent>, [||])) }
                // { PaymentDay = 179<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (15651L<Cent>, [||])) }
                { PaymentDay = 201<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 12489L<Cent>) }
                { PaymentDay = 234<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 12489L<Cent>) }
            |]

            let actual =
                voption {
                    let originalFinalPaymentDay = originalScheduledPayments |> Array.tryLast |> Option.map _.PaymentDay |> Option.defaultValue 0<OffsetDay>
                    let quoteSp =
                        { sp with
                            AsOfDate = Date(2024, 3, 6)
                            ScheduleType = ScheduleType.Rescheduled originalFinalPaymentDay
                            PaymentSchedule =
                                [|
                                    originalScheduledPayments
                                    [|
                                        { PaymentDay = 201<OffsetDay>; PaymentDetails = ScheduledPayment (ScheduledPaymentType.Rescheduled 12489L<Cent>) }
                                        { PaymentDay = 232<OffsetDay>; PaymentDetails = ScheduledPayment (ScheduledPaymentType.Rescheduled 12489L<Cent>) }
                                        { PaymentDay = 262<OffsetDay>; PaymentDetails = ScheduledPayment (ScheduledPaymentType.Rescheduled 12489L<Cent>) }
                                        { PaymentDay = 293<OffsetDay>; PaymentDetails = ScheduledPayment (ScheduledPaymentType.Rescheduled 12489L<Cent>) }
                                        { PaymentDay = 323<OffsetDay>; PaymentDetails = ScheduledPayment (ScheduledPaymentType.Rescheduled 12489L<Cent>) }
                                        { PaymentDay = 354<OffsetDay>; PaymentDetails = ScheduledPayment (ScheduledPaymentType.Rescheduled 79109L<Cent>) }
                                    |]
                                |]
                                |> Array.concat
                                |> IrregularSchedule
                        }
                    let! quote = getQuote Settlement quoteSp actualPayments
                    let title = "5) Checking that the fees refund behaves correctly"
                    let original = originalScheduledPayments |> Formatting.generateHtmlFromArray None
                    let revised = quote.RevisedSchedule.ScheduleItems |> Formatting.generateHtmlFromArray None
                    $"{title}<br /><br />{original}<br />{revised}" |> Formatting.outputToFile' "out/UnitPeriodConfigTest005.md"
                    return quote.RevisedSchedule.FinalApr |> finalAprPercent
                }

            let expected = ValueSome (Percent 699.52500m)
            actual |> should equal expected

