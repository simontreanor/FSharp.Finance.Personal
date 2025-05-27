namespace FSharp.Finance.Personal.Tests

open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal
open Amortisation
open AppliedPayment
open Calculation
open DateDay
open Scheduling
open UnitPeriod

module Amortisation2Tests =

    let folder = "Amortisation2"

    let quickActualPayments (days: int array) levelPayment finalPayment =
        days
        |> Array.rev
        |> Array.splitAt 1
        |> fun (last, rest) -> [|
            last
            |> Array.map (fun d -> (d * 1<OffsetDay>), [| ActualPayment.quickConfirmed finalPayment |])
            rest
            |> Array.map (fun d -> (d * 1<OffsetDay>), [| ActualPayment.quickConfirmed levelPayment |])
        |]
        |> Array.concat
        |> Array.rev
        |> Map.ofArray


    let quickExpectedFinalItem date offsetDay paymentValue interestAdjustment interestPortion principalPortion =
        offsetDay,
        {
            OffsetDayType = OffsetDayType.EvaluationDay
            OffsetDate = date
            Advances = [||]
            ScheduledPayment = ScheduledPayment.quick (ValueSome paymentValue) ValueNone
            Window = 5
            PaymentDue = paymentValue
            ActualPayments = [|
                {
                    ActualPaymentStatus = ActualPaymentStatus.Confirmed paymentValue
                    Metadata = Map.empty
                }
            |]
            GeneratedPayment = NoGeneratedPayment
            NetEffect = paymentValue
            PaymentStatus = PaymentMade
            BalanceStatus = ClosedBalance
            ActuarialInterest = interestAdjustment
            NewInterest = interestAdjustment
            NewCharges = [||]
            PrincipalPortion = principalPortion
            FeePortion = 0L<Cent>
            InterestPortion = interestPortion
            ChargesPortion = 0L<Cent>
            FeeRebate = 0L<Cent>
            PrincipalBalance = 0L<Cent>
            FeeBalance = 0L<Cent>
            InterestBalance = 0m<Cent>
            ChargesBalance = 0L<Cent>
            SettlementFigure = 0L<Cent>
            FeeRebateIfSettled = 0L<Cent>
        }

    let parameters1: Parameters = {
        Basic = {
            EvaluationDate = Date(2023, 3, 31)
            StartDate = Date(2022, 11, 26)
            Principal = 1500_00L<Cent>
            ScheduleConfig =
                AutoGenerateSchedule {
                    UnitPeriodConfig = Monthly(1, 2022, 11, 31)
                    ScheduleLength = PaymentCount 5
                }
            PaymentConfig = {
                LevelPaymentOption = LowerFinalPayment
                Rounding = RoundUp
            }
            FeeConfig = ValueNone
            InterestConfig = {
                Method = Interest.Method.Actuarial
                StandardRate = Interest.Rate.Daily(Percent 0.8m)
                Cap = {
                    TotalAmount = Amount.Percentage(Percent 100m, Restriction.NoLimit)
                    DailyAmount = Amount.Percentage(Percent 0.8m, Restriction.NoLimit)
                }
                Rounding = RoundDown
                AprMethod = Apr.CalculationMethod.UnitedKingdom 3
            }
        }
        Advanced = {
            PaymentConfig = {
                ScheduledPaymentOption = AsScheduled
                Minimum = DeferOrWriteOff 50L<Cent>
                Timeout = 3<DurationDay>
            }
            FeeConfig = ValueNone
            ChargeConfig =
                Some {
                    ChargeTypes =
                        Map [
                            Charge.LatePayment,
                            {
                                Value = 10_00L<Cent>
                                ChargeGrouping = Charge.ChargeGrouping.OneChargeTypePerDay
                                ChargeHolidays = [||]
                            }
                        ]
                }
            InterestConfig = {
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = Interest.Rate.Zero
            }
            SettlementDay = SettlementDay.NoSettlement
            TrimEnd = false
        }
    }

    [<Fact>]
    let ActualPaymentTest000 () =
        let title = "ActualPaymentTest000"

        let description =
            "Standard schedule with month-end payments from 4 days and paid off on time"

        let actualPayments =
            quickActualPayments [| 4; 35; 66; 94; 125 |] 456_88L<Cent> 456_84L<Cent>

        let basicSchedule = calculateBasicSchedule parameters1.Basic

        let scheduledPayments =
            basicSchedule.Items
            |> Array.filter (_.ScheduledPayment >> ScheduledPayment.isSome)
            |> Array.map (fun si ->
                si.Day,
                {
                    si.ScheduledPayment with
                        Original = si.ScheduledPayment.Original
                }
            )
            |> Map.ofArray

        let schedule =
            Amortisation2.calculateSchedule parameters1 actualPayments scheduledPayments None

        Amortisation.Schedule.outputHtmlToFile folder title description parameters1 "" {
            AmortisationSchedule = {
                Unchecked.defaultof<Amortisation.Schedule> with
                    ScheduleItems = schedule
            }
            BasicSchedule = Unchecked.defaultof<BasicSchedule>
        }

        let actual = schedule |> Map.maxKeyValue

        let expected =
            quickExpectedFinalItem
                (Date(2023, 3, 31))
                125<OffsetDay>
                456_84L<Cent>
                90_78.288m<Cent>
                90_78L<Cent>
                366_06L<Cent>

        actual |> should equal expected
