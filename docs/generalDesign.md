---
title: General design considerations
category: Technical
categoryindex: 3
index: 1
description: A look into some of the technical design decisions
keywords: technical design
---

# General Design Considerations

## Integer amounts

The library defines the unit of measure `[<Measure>] type Cent` and currency amounts are represented as `int64<Cent>`. Why is this? Why not use plain decimals?

Firstly, having the unit of measure avoids mixing different numerical types unless we specifically need to. Secondly, restricting values to (long) integers means that we don't represent fractional currency base units in the calculations. As a general principal, whole units of cents (or pence, or øre) are what customers expect to see. In amortisation schedules, this forces us to deal explicitly with the rounding when performing the calculations, and this in turn makes the calculations much more predictable, and also verifiable by the customer. The amounts they see (which are always whole base units) are the amounts the calculations are based on. And as a bonus, why `int64` instead of plain old `int`? Well, have you seen the rate of inflation lately?

## Dates with no times

Payments are grouped by days in amortisation schedule calculations, so times are irrelevant, and interest is calculated on whole numbers of days. Similarly, with APR calculations, the unit periods are based on whole numbers of days. So we remove the time component to simplify matters, and that means it is the responsibility of the calling application to determine the relevant dates, taking into account the customer's time zone. Given the complexities surrounding time-zone conversions, this is best for keeping this library as simple and reliable as possible.

I would have relied on `DateOnly` but this is not compatible with .NET Standard 2.0, so this library contains an equivalent called `Date` that works fine even on the obsolescent .NET Framework.
