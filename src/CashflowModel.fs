namespace FSharp.Finance.B2B

open FSharp.Finance.Personal

/// Future-facing cashflow modeling types for potential integration
module CashflowModel =

    open DateDay
    open Calculation

    /// Types of cashflows for modeling purposes
    type CashflowType =
        /// Initial loan advance or credit extension
        | Advance
        /// Regular scheduled payment (principal + interest)
        | ScheduledPayment
        /// Interest-only payment
        | InterestPayment
        /// Principal-only payment
        | PrincipalPayment
        /// Fee payment
        | FeePayment
        /// Penalty or charge payment
        | ChargePayment
        /// Early settlement payment
        | SettlementPayment
        /// Refund to customer
        | Refund

    /// A cashflow event record for future modeling
    type CashflowEvent = {
        /// Unique identifier for the cashflow event
        Id: string
        /// The date of the cashflow
        Date: Date
        /// The type of cashflow
        CashflowType: CashflowType
        /// The amount in cents (positive for outflows from customer, negative for inflows to customer)
        Amount: int64<Cent>
        /// Optional description
        Description: string option
        /// Optional reference to related cashflow events
        RelatedEvents: string array
    }

    /// Cashflow event helper functions
    module CashflowEvent =
        
        /// Create a basic cashflow event
        let create id date cashflowType amount description =
            {
                Id = id
                Date = date
                CashflowType = cashflowType
                Amount = amount
                Description = description
                RelatedEvents = [||]
            }

        /// Create an advance cashflow event
        let advance id date amount description =
            create id date Advance amount description

        /// Create a scheduled payment cashflow event
        let scheduledPayment id date amount description =
            create id date ScheduledPayment amount description

        /// Create a settlement payment cashflow event
        let settlement id date amount description =
            create id date SettlementPayment amount description

    // TODO: Future enhancement - integrate with core engine for comprehensive cashflow modeling
    // This module is intentionally not yet wired into the core calculation engine
    // but provides a foundation for future cashflow-based analytics
