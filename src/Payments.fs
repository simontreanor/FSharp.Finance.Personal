namespace FSharp.Finance.Personal

module Payments =

    /// either an extra scheduled payment (e.g. for a restructured payment plan) or an actual payment made, optionally with charges
    [<Struct>]
    type PaymentDetails =
        /// the amount of any extra scheduled payment due on the current day
        | ScheduledPayment of ScheduledPayment: int64<Cent>
        /// the amounts of any actual payments made on the current day, with any charges incurred
        | ActualPayment of ActualPayment: int64<Cent> * Charges: Charge array

    /// a payment (either extra scheduled or actually paid) to be applied to a payment schedule
    [<Struct>]
    type Payment = {
        /// the day the payment is made, as an offset of days from the start date
        PaymentDay: int<OffsetDay>
        /// the details of the payment
        PaymentDetails: PaymentDetails
    }
 
    [<Struct>]
    type PaymentStatus =
        /// a scheduled payment was made in full and on time
        | PaymentMade
        /// a scheduled payment was missed completely
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
        | WithinGracePeriod
