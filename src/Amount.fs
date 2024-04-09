namespace FSharp.Finance.Personal

module Amount =

    open Calculation
    open Currency
    open Percentages

    /// the type of restriction placed on a possible value
    [<RequireQualifiedAccess; Struct>]
    type Restriction =
        /// prevent values below a certain limit
        | LowerLimit of LowerLimit:int64<Cent>
        /// prevent values above a certain limit
        | UpperLimit of UpperLimit:int64<Cent>
        /// constrain values to within a range
        | WithinRange of MinValue:int64<Cent> * MaxValue:int64<Cent>
        with
            /// calculate a permitted value based on a restriction
            static member calculate restriction amount =
                match restriction with
                | ValueSome (LowerLimit a) -> amount |> max (decimal a)
                | ValueSome (UpperLimit a) -> amount |> min (decimal a)
                | ValueSome (WithinRange (lower, upper)) -> amount |> min (decimal upper) |> max (decimal lower)
                | ValueNone -> amount

    /// an amount specified either as a simple amount or as a percentage of another amount, optionally restricted to lower and/or upper limits
    [<Struct>]
    type Amount =
        /// a fixed fee
        | Simple of Simple:int64<Cent>
        /// a percentage of the principal, optionally restricted
        | Percentage of Percentage:Percent * Restriction:Restriction voption * Rounding:Rounding voption
        with
            /// calculates the total amount based on any restrictions
            static member total (baseAmount: int64<Cent>) amount =
                match amount with
                | Percentage (Percent percentage, restriction, rounding) ->
                    decimal baseAmount * decimal percentage / 100m
                    |> Restriction.calculate restriction
                    |> Rounding.round rounding
                | Simple simple -> decimal simple
                |> ( * ) 1m<Cent>
