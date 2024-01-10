namespace FSharp.Finance.Personal.Tests

open System
open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal

module AprUnitedKingdomTests =

    open Apr
    module Quirky =

        [<Fact>]
        let ``APR calculation 1 payment zero`` () =
            UnitedKingdom.calculateApr (Date(2012, 10, 10)) 50000L<Cent> [| { TransferType = Payment; TransferDate = Date(2012, 10, 10); Amount = 50000L<Cent> } |]
            |> getAprOr -1m
            |> should (equalWithin 0.001) 0m

        [<Fact>] 
        let``APR calculation 1 payment`` () =
            UnitedKingdom.calculateApr (Date(2012, 10, 10)) 50000L<Cent> [| { TransferType = Payment; TransferDate = Date(2012, 10, 15); Amount = 51000L<Cent> } |]
            |> getAprOr 0m
            |> should (equalWithin 0.001) (Percent 324.436m |> Percent.toDecimal)

        [<Fact>] 
        let``APR calculation 2 payments`` () =
            UnitedKingdom.calculateApr (Date(2012, 10, 10)) 50000L<Cent> [| { TransferType = Payment; TransferDate = Date(2012, 11, 10); Amount = 27000L<Cent> }; { TransferType = Payment; TransferDate = Date(2012, 12, 10); Amount = 27000L<Cent> } |]
            |> getAprOr 0m
            |> should (equalWithin 0.001) (Percent 84.63m |> Percent.toDecimal)
