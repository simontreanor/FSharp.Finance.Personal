namespace FSharp.Finance.Personal

/// categorising the types of incoming payments based on whether they are scheduled, actual or generated
module CustomerPayments =

    open Currency
    open DateDay
    open FeesAndCharges

    /// any original or rescheduled payment, affecting how any payment due is calculated
    type ScheduledPayment =
        {
            Original: int64<Cent> voption
            Rescheduled: int64<Cent> voption
            Adjustment: int64<Cent> voption
            /// the original simple interest
            OriginalSimpleInterest: int64<Cent>
            /// the original, contractually calculated interest
            ContractualInterest: decimal<Cent>
            Metadata: Map<string, obj>
        }
        with
            /// the total amount of the payment
            member x.Total =
                match x.Original, x.Rescheduled with 
                | _, ValueSome ra -> ra
                | ValueSome oa, ValueNone -> oa
                | ValueNone, ValueNone -> 0L<Cent>
            member x.IsSome =
                x.Original.IsSome || x.Rescheduled.IsSome
            member x.AmendedTotal =
                x.Total + ValueOption.defaultValue 0L<Cent> x.Adjustment
            static member DefaultValue =
                { Original = ValueNone; Rescheduled = ValueNone; Adjustment = ValueNone; OriginalSimpleInterest = 0L<Cent>; ContractualInterest = 0m<Cent>; Metadata = Map.empty }
            static member Quick originalAmount rescheduledAmount =
                { Original = originalAmount; Rescheduled = rescheduledAmount; Adjustment = ValueNone; OriginalSimpleInterest = 0L<Cent>; ContractualInterest = 0m<Cent>; Metadata = Map.empty }

    /// the status of the payment, allowing for delays due to payment-provider processing times
    [<RequireQualifiedAccess; Struct>]
    type ActualPaymentStatus =
        /// a write-off payment has been applied
        | WriteOff of WriteOff: int64<Cent>
        /// the payment has been initiated but is not yet confirmed
        | Pending of Pending: int64<Cent>
        /// the payment had been initiated but was not confirmed within the timeout
        | TimedOut of TimedOut: int64<Cent>
        /// the payment has been confirmed
        | Confirmed of Confirmed: int64<Cent>
        /// the payment has been failed, with optional charges (e.g. due to insufficient-funds penalties)
        | Failed of Failed: int64<Cent> * Charges: Charge array
        with
            /// the total amount of the payment
            static member Total = function
                | WriteOff ap -> ap
                | Pending ap -> ap
                | TimedOut _ -> 0L<Cent>
                | Confirmed ap -> ap
                | Failed _ -> 0L<Cent>

    type ActualPayment =
        {
            ActualPaymentStatus: ActualPaymentStatus
            Metadata: Map<string, obj>
        }
        with
            static member total = function { ActualPaymentStatus = aps } -> ActualPaymentStatus.Total aps
            static member QuickConfirmed amount = { ActualPaymentStatus = ActualPaymentStatus.Confirmed amount; Metadata = Map.empty }
            static member QuickPending amount = { ActualPaymentStatus = ActualPaymentStatus.Pending amount; Metadata = Map.empty }
            static member QuickFailed amount charges = { ActualPaymentStatus = ActualPaymentStatus.Failed (amount, charges); Metadata = Map.empty }
            static member QuickWriteOff amount = { ActualPaymentStatus = ActualPaymentStatus.WriteOff amount; Metadata = Map.empty }

    /// a payment (either extra scheduled or actually paid) to be applied to a payment schedule
    type CustomerPayment = {
        /// the amount of any extra scheduled payment due on the current day
        ScheduledPayment: ScheduledPayment voption
        /// the amounts of any actual payments made on the current day, with any charges incurred
        ActualPayment: ActualPayment voption
        /// the amounts of any generated payments made on the current day and their type
        GeneratedPayment: int64<Cent> voption
     }
 
    /// the status of a payment made by the customer
    [<Struct>]
    type CustomerPaymentStatus =
        /// no payment is required on the specified day
        | NoneScheduled
        /// a payment has been initiated but not yet confirmed
        | PaymentPending
        /// a scheduled payment was made in full and on time
        | PaymentMade
        /// no payment is due on the specified day because of earlier extra-/overpayments
        | NothingDue
        /// a scheduled payment is not paid on time, but is paid within the window
        | PaidLaterInFull
        /// a scheduled payment is not paid on time, but is partially paid within the window
        | PaidLaterOwing of Shortfall: int64<Cent>
        /// a scheduled payment was missed completely, i.e. not paid within the window
        | MissedPayment
        /// a scheduled payment was made on time but not in the full amount
        | Underpayment
        /// a scheduled payment was made on time but exceeded the full amount
        | Overpayment
        /// a payment was made on a day when no payments were scheduled
        | ExtraPayment
        /// a refund was processed
        | Refunded
        /// a scheduled payment is in the future (seen from the as-of date)
        | NotYetDue
        /// a scheduled payment has not been made on time but is within the late-charge grace period
        | PaymentDue
        /// a payment generated by a settlement quote
        | Generated
        /// no payment needed because the loan has already been settled
        | NoLongerRequired
        /// a schedule item generated to show the balances on the as-of date
        | InformationOnly

    /// the type of the scheduled; for scheduled payments, this affects how any payment due is calculated
    [<RequireQualifiedAccess; Struct>]
    type ScheduleType =
        /// an original schedule
        | Original
        /// a schedule based on a previous one
        | Rescheduled

    /// a regular schedule based on a unit-period config with a specific number of payments of a specified amount
    [<RequireQualifiedAccess; Struct>]
    type RegularFixedSchedule = {
        UnitPeriodConfig: UnitPeriod.Config
        PaymentCount: int
        PaymentAmount: int64<Cent>
        ScheduleType: ScheduleType
    }

    /// whether a payment plan is generated according to a regular schedule or is an irregular array of payments
    type CustomerPaymentSchedule =
        /// a regular schedule based on a unit-period config with a specific number of payments with an auto-calculated amount
        | RegularSchedule of UnitPeriodConfig: UnitPeriod.Config * PaymentCount: int * MaxDuration: Duration voption
        /// a regular schedule based on one or more unit-period configs each with a specific number of payments of a specified amount
        | RegularFixedSchedule of RegularFixedSchedule array
        /// just a bunch of payments
        | IrregularSchedule of IrregularSchedule: Map<int<OffsetDay>, ScheduledPayment>
