# FSharp.Finance.Personal

# Changelog

## [Unreleased]

- Planned: Add payment mapping to match scheduled and actual payments

---

## [2.5.1] - 2025-05-27

### Migration Guide

- `UnitPeriod.Single` is no longer a valid config
- `RegularSchedule` is now `AutoGenerateSchedule`, and the `ScheduleLength` is either a payment count or a maximum length, but not both
- The `Simple` interest method is now `Actuarial`
- `Parameters` has been split into `Basic` and `Advanced`, with only Basic required for basic schedules but both required for full amortisation schedules
- Schedules are now represented as maps with the offset day as the key
- There is now `PaymentQuote.Apportionment` giving a full breakdown of charges, interest, fees and principal
- HTML output is now handled much better, with the option to provide a description for the output, and column visibility is now automatic

---

## [1.3.5] - 2024-09-18

- This version is the final 1.x.x release.
