namespace FSharp.Finance.Personal.Tests

open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal

module UnitPeriodConfigTests =

    open UnitPeriod
    open CustomerPayments
    open PaymentSchedule
    open Amortisation
    open Quotes

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

        [<Fact>]
        let ``3. Irregular payment schedule does not break detect function`` () =
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
                    quote.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/UnitPeriodConfigTest003.md"
                    return quote.RevisedSchedule.FinalApr
                }

            let expected = ValueSome (ValueSome (Solution.Found (0.5651076300779573469886448328m, 46, 0), ValueSome (Percent 56.51100m)))
            actual |> should equal expected
