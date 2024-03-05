namespace FSharp.Finance.Personal.Tests

open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal

module UnitPeriodConfigTests =

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

    open CustomerPayments
    open PaymentSchedule
    open Amortisation
    open Quotes

    module ConfigEdges =

        let finalAprPercent = function
        | ValueSome (Solution.Found _, ValueSome percent) -> percent
        | _ -> Percent 0m

        [<Fact>]
        let ``1) Irregular payment schedule does not break detect function`` () =
            let sp = {
                    AsOfDate = Date(2024, 3, 5)
                    StartDate = Date(2022, 5, 5)
                    Principal = 100000L<Cent>
                    PaymentSchedule = RegularSchedule(Weekly(2, Date(2022, 5, 13)), 12)
                    FeesAndCharges = {
                        Fees = [| Fee.CabOrCsoFee (Amount.Percentage (Percent 154.47m, ValueNone)) |]
                        FeesSettlement = Fees.Settlement.ProRataRefund
                        Charges = [||]
                        ChargesHolidays = [||]
                        LatePaymentGracePeriod = 3<DurationDay>
                    }
                    Interest = {
                        Rate = Interest.Rate.Annual (Percent 9.95m)
                        Cap = { Total = ValueNone; Daily = ValueNone }
                        InitialGracePeriod = 3<DurationDay>
                        Holidays = [||]
                        RateOnNegativeBalance = ValueNone
                    }
                    Calculation = {
                        AprMethod = Apr.CalculationMethod.UsActuarial 5
                        RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                        MinimumPayment = DeferOrWriteOff 50L<Cent>
                        PaymentTimeout = 3<DurationDay>
                        NegativeInterestOption = ApplyNegativeInterest
                    }
                }
            
            let actualPayments = [|
                { PaymentDay =  8<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Confirmed 21700L<Cent>) }
                { PaymentDay =  22<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (21700L<Cent>, [||])) }
                { PaymentDay =  22<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (21700L<Cent>, [||])) }
                { PaymentDay =  22<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (21700L<Cent>, [||])) }
                { PaymentDay =  25<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Confirmed 21700L<Cent>) }
                { PaymentDay =  39<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (21700L<Cent>, [||])) }
                { PaymentDay =  39<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (21700L<Cent>, [||])) }
                { PaymentDay =  39<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (21700L<Cent>, [||])) }
                { PaymentDay =  45<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (21700L<Cent>, [||])) }
                { PaymentDay =  45<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Confirmed 21700L<Cent>) }
                { PaymentDay =  50<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (21700L<Cent>, [||])) }
                { PaymentDay =  50<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (21700L<Cent>, [||])) }
                { PaymentDay =  50<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (21700L<Cent>, [||])) }
                { PaymentDay =  53<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (21700L<Cent>, [||])) }
                { PaymentDay =  53<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (21700L<Cent>, [||])) }
                { PaymentDay =  53<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (21700L<Cent>, [||])) }
                { PaymentDay =  56<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Confirmed 21700L<Cent>) }
                { PaymentDay =  67<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (21700L<Cent>, [||])) }
                { PaymentDay =  67<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (21700L<Cent>, [||])) }
                { PaymentDay =  67<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (21700L<Cent>, [||])) }
                { PaymentDay =  73<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (21700L<Cent>, [||])) }
                { PaymentDay =  73<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (21700L<Cent>, [||])) }
                { PaymentDay =  73<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (21700L<Cent>, [||])) }
                { PaymentDay =  78<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (21700L<Cent>, [||])) }
                { PaymentDay =  78<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (21700L<Cent>, [||])) }
                { PaymentDay =  78<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (21700L<Cent>, [||])) }
                { PaymentDay =  79<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (21700L<Cent>, [||])) }
                { PaymentDay =  79<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (21700L<Cent>, [||])) }
                { PaymentDay =  79<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (21700L<Cent>, [||])) }
                { PaymentDay =  274<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (26036L<Cent>, [||])) }
                { PaymentDay =  302<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (26036L<Cent>, [||])) }
                { PaymentDay =  330<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Confirmed 21700L<Cent>) }
            |]

            let actual =
                voption {
                    let! quote = getQuote Settlement sp actualPayments
                    quote.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/UnitPeriodConfigTest001.md"
                    return quote.RevisedSchedule.FinalApr |> finalAprPercent
                }

            let expected = ValueSome (Percent 56.51100m)
            actual |> should equal expected

        [<Fact>]
        let ``2) Irregular payment schedule does not break APR calculation`` () =
            let sp = {
                AsOfDate = Date(2024, 3, 5)
                StartDate = Date(2023, 4, 13)
                Principal = 70000L<Cent>
                PaymentSchedule = RegularSchedule(Weekly(2, Date(2023, 4, 20)), 12)
                FeesAndCharges = {
                    Fees = [| Fee.CabOrCsoFee (Amount.Percentage (Percent 154.47m, ValueNone)) |]
                    FeesSettlement = Fees.Settlement.ProRataRefund
                    Charges = [||]
                    ChargesHolidays = [||]
                    LatePaymentGracePeriod = 3<DurationDay>
                }
                Interest = {
                    Rate = Interest.Rate.Annual (Percent 9.95m)
                    Cap = { Total = ValueNone; Daily = ValueNone }
                    InitialGracePeriod = 3<DurationDay>
                    Holidays = [||]
                    RateOnNegativeBalance = ValueNone
                }
                Calculation = {
                    AprMethod = Apr.CalculationMethod.UsActuarial 5
                    RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                    MinimumPayment = DeferOrWriteOff 50L<Cent>
                    PaymentTimeout = 3<DurationDay>
                    NegativeInterestOption = ApplyNegativeInterest
                }
            }

            let actualPayments = [|
                { PaymentDay =  6<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Confirmed 17275L<Cent>) }
                { PaymentDay =  21<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Confirmed 17275L<Cent>) }
                { PaymentDay =  35<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Confirmed 17275L<Cent>) }
                { PaymentDay =  49<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (17275L<Cent>, [||])) }
                { PaymentDay =  49<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (17275L<Cent>, [||])) }
                { PaymentDay =  50<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Confirmed 17275L<Cent>) }
                { PaymentDay =  64<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Confirmed 17275L<Cent>) }
                { PaymentDay =  80<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Confirmed 17275L<Cent>) }
                { PaymentDay =  94<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Confirmed 17488L<Cent>) }
                { PaymentDay =  108<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (17488L<Cent>, [||])) }
                { PaymentDay =  108<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (17488L<Cent>, [||])) }
                { PaymentDay =  109<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (17488L<Cent>, [||])) }
                { PaymentDay =  111<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (17701L<Cent>, [||])) }
                { PaymentDay =  111<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (17701L<Cent>, [||])) }
                { PaymentDay =  112<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (17772L<Cent>, [||])) }
                { PaymentDay =  112<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (17772L<Cent>, [||])) }
                { PaymentDay =  112<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Confirmed 17772L<Cent>) }
                { PaymentDay =  122<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Confirmed 17488L<Cent>) }
                { PaymentDay =  128<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Confirmed 23521L<Cent>) }
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
                StartDate = Date(2023, 1, 20)
                Principal = 65000L<Cent>
                PaymentSchedule = RegularSchedule(Weekly(2, Date(2023, 2, 2)), 11)
                FeesAndCharges = {
                    Fees = [| Fee.CabOrCsoFee (Amount.Percentage (Percent 154.47m, ValueNone)) |]
                    FeesSettlement = Fees.Settlement.ProRataRefund
                    Charges = [||]
                    ChargesHolidays = [||]
                    LatePaymentGracePeriod = 3<DurationDay>
                }
                Interest = {
                    Rate = Interest.Rate.Annual (Percent 9.95m)
                    Cap = { Total = ValueNone; Daily = ValueNone }
                    InitialGracePeriod = 3<DurationDay>
                    Holidays = [||]
                    RateOnNegativeBalance = ValueNone
                }
                Calculation = {
                    AprMethod = Apr.CalculationMethod.UsActuarial 5
                    RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                    MinimumPayment = DeferOrWriteOff 50L<Cent>
                    PaymentTimeout = 3<DurationDay>
                    NegativeInterestOption = ApplyNegativeInterest
                }
            }

            let actualPayments = [|
                { PaymentDay =  13<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (17494L<Cent>, [||])) }
                { PaymentDay =  13<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (17494L<Cent>, [||])) }
                { PaymentDay =  14<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (17494L<Cent>, [||])) }
                { PaymentDay =  16<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (17494L<Cent>, [||])) }
                { PaymentDay =  16<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (17494L<Cent>, [||])) }
                { PaymentDay =  17<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (17494L<Cent>, [||])) }
                { PaymentDay =  19<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Confirmed 17494L<Cent>) }
                { PaymentDay =  27<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (17494L<Cent>, [||])) }
                { PaymentDay =  27<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (17494L<Cent>, [||])) }
                { PaymentDay =  28<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (17494L<Cent>, [||])) }
                { PaymentDay =  30<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (17494L<Cent>, [||])) }
                { PaymentDay =  30<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (17494L<Cent>, [||])) }
                { PaymentDay =  31<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (17494L<Cent>, [||])) }
                { PaymentDay =  33<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (17494L<Cent>, [||])) }
                { PaymentDay =  33<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (17494L<Cent>, [||])) }
                { PaymentDay =  34<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (17494L<Cent>, [||])) }
                { PaymentDay =  36<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Confirmed 17494L<Cent>) }
                { PaymentDay =  41<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (17494L<Cent>, [||])) }
                { PaymentDay =  41<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (17494L<Cent>, [||])) }
                { PaymentDay =  42<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (17494L<Cent>, [||])) }
                { PaymentDay =  44<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (17494L<Cent>, [||])) }
                { PaymentDay =  44<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (17494L<Cent>, [||])) }
                { PaymentDay =  45<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (17494L<Cent>, [||])) }
                { PaymentDay =  47<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (17494L<Cent>, [||])) }
                { PaymentDay =  47<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (17494L<Cent>, [||])) }
                { PaymentDay =  48<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (17494L<Cent>, [||])) }
                { PaymentDay =  55<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (17494L<Cent>, [||])) }
                { PaymentDay =  55<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Confirmed 17494L<Cent>) }
                { PaymentDay =  56<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (17494L<Cent>, [||])) }
                { PaymentDay =  58<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (17494L<Cent>, [||])) }
                { PaymentDay =  58<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (17494L<Cent>, [||])) }
                { PaymentDay =  59<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Confirmed 17494L<Cent>) }
                { PaymentDay =  69<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (17494L<Cent>, [||])) }
                { PaymentDay =  69<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (17494L<Cent>, [||])) }
                { PaymentDay =  70<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (17494L<Cent>, [||])) }
                { PaymentDay =  72<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Confirmed 17494L<Cent>) }
                { PaymentDay =  83<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (17494L<Cent>, [||])) }
                { PaymentDay =  83<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (17494L<Cent>, [||])) }
                { PaymentDay =  84<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (17494L<Cent>, [||])) }
                { PaymentDay =  86<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Confirmed 17620L<Cent>) }
                { PaymentDay =  97<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Confirmed 17494L<Cent>) }
                { PaymentDay =  111<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (17494L<Cent>, [||])) }
                { PaymentDay =  112<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (17494L<Cent>, [||])) }
                { PaymentDay =  114<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Confirmed 17721L<Cent>) }
                { PaymentDay =  125<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (17494L<Cent>, [||])) }
                { PaymentDay =  126<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Failed (17494L<Cent>, [||])) }
                { PaymentDay =  128<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Confirmed 17721L<Cent>) }
                { PaymentDay =  132<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Confirmed 19995L<Cent>) }
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
                StartDate = Date(2022, 10, 13)
                Principal = 50000L<Cent>
                PaymentSchedule = RegularSchedule(Weekly(2, Date(2022, 10, 28)), 11)
                FeesAndCharges = {
                    Fees = [| Fee.CabOrCsoFee (Amount.Percentage (Percent 154.47m, ValueNone)) |]
                    FeesSettlement = Fees.Settlement.ProRataRefund
                    Charges = [||]
                    ChargesHolidays = [||]
                    LatePaymentGracePeriod = 3<DurationDay>
                }
                Interest = {
                    Rate = Interest.Rate.Annual (Percent 9.95m)
                    Cap = { Total = ValueNone; Daily = ValueNone }
                    InitialGracePeriod = 3<DurationDay>
                    Holidays = [||]
                    RateOnNegativeBalance = ValueNone
                }
                Calculation = {
                    AprMethod = Apr.CalculationMethod.UsActuarial 5
                    RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                    MinimumPayment = DeferOrWriteOff 50L<Cent>
                    PaymentTimeout = 3<DurationDay>
                    NegativeInterestOption = ApplyNegativeInterest
                }
            }

            let actualPayments = [|
                { PaymentDay =  12<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Confirmed 13465L<Cent>) }
                { PaymentDay =  26<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Confirmed 13465L<Cent>) }
                { PaymentDay =  41<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Confirmed 13465L<Cent>) }
                { PaymentDay =  54<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Confirmed 13465L<Cent>) }
                { PaymentDay =  73<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Confirmed 13465L<Cent>) }
                { PaymentDay =  88<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Confirmed 13562L<Cent>) }
                { PaymentDay =  101<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Confirmed 13580L<Cent>) }
                { PaymentDay =  117<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Confirmed 13695L<Cent>) }
                { PaymentDay =  127<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Confirmed 13465L<Cent>) }
                { PaymentDay =  134<OffsetDay>; PaymentDetails = ActualPayment (PaymentStatus.Confirmed 15560L<Cent>) }
            |]

            let actual =
                voption {
                    let! quote = getQuote Settlement sp actualPayments
                    quote.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/UnitPeriodConfigTest004.md"
                    return quote.RevisedSchedule.FinalApr |> finalAprPercent
                }

            let expected = ValueSome (Percent 930.55900m)
            actual |> should equal expected
