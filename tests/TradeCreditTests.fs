namespace FSharp.Finance.Personal.Tests

open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal
open FSharp.Finance.B2B
open FSharp.Finance.B2B.TradeCredit

module TradeCreditTests =

    [<Fact>]
    let ``Trade credit 2/10 net 30 implied simple annual rate test`` () =
        // Arrange
        let terms = DiscountTerms.createTerms 2m 10 30
        
        // Act
        let impliedRate = DiscountTerms.impliedAnnualRateSimple terms
        
        // Assert
        // Expected calculation: (2% / (100% - 2%)) * (365 / (30 - 10))
        // = (0.02 / 0.98) * (365 / 20)
        // = 0.020408163 * 18.25
        // = 0.372448979 (≈ 37.24%)
        impliedRate |> should (equalWithin 0.0001m) 0.3724m

    [<Fact>]
    let ``Trade credit 1/15 net 45 implied simple annual rate test`` () =
        // Arrange
        let terms = DiscountTerms.createTerms 1m 15 45
        
        // Act
        let impliedRate = DiscountTerms.impliedAnnualRateSimple terms
        
        // Assert
        // Expected calculation: (1% / (100% - 1%)) * (365 / (45 - 15))
        // = (0.01 / 0.99) * (365 / 30)
        // = 0.010101010 * 12.166667
        // = 0.122893939 (≈ 12.29%)
        impliedRate |> should (equalWithin 0.0001m) 0.1229m

    [<Fact>]
    let ``Trade credit compounded rate calculation test`` () =
        // Arrange
        let terms = DiscountTerms.createTerms 2m 10 30
        
        // Act
        let compoundedRate = DiscountTerms.impliedAnnualRateCompounded terms
        
        // Assert
        // This should be higher than the simple rate due to compounding
        compoundedRate |> should be (greaterThan 0.37m)
        compoundedRate |> should be (lessThan 0.45m)

    [<Fact>]
    let ``Trade credit zero discount rate returns zero`` () =
        // Arrange
        let terms = DiscountTerms.createTerms 0m 10 30
        
        // Act
        let simpleRate = DiscountTerms.impliedAnnualRateSimple terms
        let compoundedRate = DiscountTerms.impliedAnnualRateCompounded terms
        
        // Assert
        simpleRate |> should equal 0m
        compoundedRate |> should equal 0m

    [<Fact>]
    let ``Trade credit createTerms validation tests`` () =
        // Test invalid discount percentage (negative)
        (fun () -> DiscountTerms.createTerms -1m 10 30 |> ignore)
        |> should throw typeof<System.ArgumentException>

        // Test invalid discount percentage (100%)
        (fun () -> DiscountTerms.createTerms 100m 10 30 |> ignore)
        |> should throw typeof<System.ArgumentException>

        // Test invalid discount percentage (over 100%)
        (fun () -> DiscountTerms.createTerms 101m 10 30 |> ignore)
        |> should throw typeof<System.ArgumentException>

        // Test invalid discount days (negative)
        (fun () -> DiscountTerms.createTerms 2m -1 30 |> ignore)
        |> should throw typeof<System.ArgumentException>

        // Test invalid net days (not greater than discount days)
        (fun () -> DiscountTerms.createTerms 2m 10 10 |> ignore)
        |> should throw typeof<System.ArgumentException>

    [<Fact>]
    let ``Trade credit compounded rate throws a descriptive exception instead of overflowing`` () =
        // 50% discount with a 1-day extension implies 2^365, which is far beyond the range of decimal
        let terms = DiscountTerms.createTerms 50m 1 2

        let ex =
            Assert.Throws<System.ArgumentException>(fun () ->
                DiscountTerms.impliedAnnualRateCompounded terms |> ignore)

        ex.Message |> should haveSubstring "too large to represent as a decimal"

    [<Fact>]
    let ``Trade credit rate functions re-validate directly constructed terms`` () =
        // DiscountTerms is a plain record, so createTerms validation can be bypassed;
        // the rate functions must guard against division by zero themselves
        let fullDiscount = { DiscountRate = 1m; DiscountPeriodDays = 10; NetPeriodDays = 30 }

        (fun () -> DiscountTerms.impliedAnnualRateSimple fullDiscount |> ignore)
        |> should throw typeof<System.ArgumentException>

        (fun () -> DiscountTerms.impliedAnnualRateCompounded fullDiscount |> ignore)
        |> should throw typeof<System.ArgumentException>

        let zeroExtension = { DiscountRate = 0.02m; DiscountPeriodDays = 30; NetPeriodDays = 30 }

        (fun () -> DiscountTerms.impliedAnnualRateSimple zeroExtension |> ignore)
        |> should throw typeof<System.ArgumentException>

        (fun () -> DiscountTerms.impliedAnnualRateCompounded zeroExtension |> ignore)
        |> should throw typeof<System.ArgumentException>

    [<Fact>]
    let ``Trade credit standard terms creation test`` () =
        // Test standard 2/10 net 30 terms
        let standard2_10 = DiscountTerms.standard2_10Net30
        standard2_10.DiscountRate |> should equal 0.02m
        standard2_10.DiscountPeriodDays |> should equal 10
        standard2_10.NetPeriodDays |> should equal 30

        // Test standard 1/15 net 45 terms
        let standard1_15 = DiscountTerms.standard1_15Net45
        standard1_15.DiscountRate |> should equal 0.01m
        standard1_15.DiscountPeriodDays |> should equal 15
        standard1_15.NetPeriodDays |> should equal 45

    [<Fact>]
    let ``Trade credit analysis functions test`` () =
        // Arrange
        let terms = DiscountTerms.createTerms 2m 10 30
        
        // Act
        let costOfNotTaking = Analysis.costOfNotTakingDiscount terms
        let effectiveRate = Analysis.effectiveAnnualRate terms
        let costPerPeriod = Analysis.costPerPeriod terms
        let breakEven = Analysis.breakEvenBorrowingRate terms
        
        // Assert
        costOfNotTaking |> should (equalWithin 0.0001m) 0.3724m
        effectiveRate |> should be (greaterThan costOfNotTaking)
        costPerPeriod |> should (equalWithin 0.0001m) 0.0204m // 2% / (100% - 2%)
        breakEven |> should equal costOfNotTaking
