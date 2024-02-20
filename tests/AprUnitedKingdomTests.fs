namespace FSharp.Finance.Personal.Tests

open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal

module AprUnitedKingdomTests =

    open Apr
    module Quirky =

        [<Fact>]
        let ``APR calculation 1 payment 0L<Cent>`` () =
            UnitedKingdom.calculateApr (Date(2012, 10, 10)) 500_00L<Cent> [| { TransferType = Payment; TransferDate = Date(2012, 10, 10); Amount = 500_00L<Cent> } |]
            |> getAprOr -1m
            |> should (equalWithin 0.001) 0m

        [<Fact>] 
        let``APR calculation 1 payment`` () =
            UnitedKingdom.calculateApr (Date(2012, 10, 10)) 500_00L<Cent> [| { TransferType = Payment; TransferDate = Date(2012, 10, 15); Amount = 510_00L<Cent> } |]
            |> getAprOr 0m
            |> should (equalWithin 0.001) (Percent 324.436m |> Percent.toDecimal)

        [<Fact>] 
        let``APR calculation 2 payments`` () =
            UnitedKingdom.calculateApr (Date(2012, 10, 10)) 500_00L<Cent> [| { TransferType = Payment; TransferDate = Date(2012, 11, 10); Amount = 270_00L<Cent> }; { TransferType = Payment; TransferDate = Date(2012, 12, 10); Amount = 270_00L<Cent> } |]
            |> getAprOr 0m
            |> should (equalWithin 0.001) (Percent 84.63m |> Percent.toDecimal)
