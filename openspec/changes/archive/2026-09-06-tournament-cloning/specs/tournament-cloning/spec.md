# Tournament Cloning — Spec

## Purpose

Define a new capability that lets an organizer start a new tournament pre-filled from an
existing tournament's structure (divisions, group sub-stages, playoff cups, cross-division cup,
points), reviewable and editable in the creation wizard before it is submitted through the
existing `/full` creation transaction. New capability; no prior spec. Zero instance data
(rosters, teams, matches, standings, sanctions, audit logs) ever carries over.

## Requirements

### Requirement: Structure-Read Endpoint Returns a Complete Cloneable Tree

The system MUST expose an additive read endpoint returning a `TournamentStructureResponse` for
a given source tournament: every division's name, category, `IsCrossDivisionCup`,
`PointsForWin`/`PointsForLoss`, `QualifiersPerGroup`, its `Stage`s (`StageType`, `Order`,
`BracketName`, `BestOf`, `RoundRobinLegs`), and its `DivisionPlayoffMapping`s. The system MUST
NOT modify `TournamentResponse` or `DivisionResponse` to expose this data.

#### Scenario: Structure read returns every division and stage of the source tournament

- GIVEN a tournament with 2 regular divisions and a cross-division cup
- WHEN the structure-read endpoint is called for that tournament
- THEN the response includes all 3 divisions, each with its full Stage list and PlayoffMappings

#### Scenario: Existing tournament/division read endpoints are unaffected

- GIVEN the new structure-read endpoint exists
- WHEN `TournamentResponse` or `DivisionResponse` is requested by any other page
- THEN their shape is unchanged from before this capability existed

### Requirement: Any Tournament Can Be a Clone Source

The system MUST allow any existing tournament — regardless of season or status, including one
from the same season currently being planned — to be selected as a clone source.

#### Scenario: Cloning from a tournament in the same season

- GIVEN a tournament already exists in the current season
- WHEN the organizer clones it while creating another tournament for the same season
- THEN the clone proceeds with no season-based restriction

### Requirement: Full Clone Reconstructs Every Regular Division's Group Sub-Stages

The reverse-mapper MUST set a zone's `subGroupCount` to the count of the source division's
non-cup Group-type stages (those with no `BracketName`), and `roundRobinLegs` to their shared
`RoundRobinLegs` value, independent of stage naming.

#### Scenario: Division with a single group stage

- GIVEN a source division with exactly one non-cup Group-type stage
- WHEN it is cloned
- THEN the resulting zone has `hasGroupStage = true` and `subGroupCount = 1`

#### Scenario: Division split into M sub-groups

- GIVEN a source division with M non-cup Group-type stages sharing the same `RoundRobinLegs`
- WHEN it is cloned
- THEN the resulting zone has `subGroupCount = M` and that shared `roundRobinLegs` value

### Requirement: Full Clone Reconstructs Each Division's Playoff Cup Configuration Exactly

For a regular (non-cross) division, the reverse-mapper MUST group cup stages by `BracketName`
and derive each `CupConfig` as follows: `qualifiers` = `toPosition - fromPosition + 1` from the
`DivisionPlayoffMapping` whose `Destination` equals that `BracketName`; `hasThirdPlace` = whether
a `ThirdPlace`-type stage exists in that group; `bestOfByStage` = each present stage type's exact
`BestOf` value. `qualifiers` MUST NOT be guessed from the derived stage-type set alone, since
multiple qualifier counts (e.g. 3 and 4) produce the same stage-type set.

#### Scenario: Cup with 4 qualifiers and a third-place decider

- GIVEN a division cup bracket with SemiFinal, ThirdPlace and Final stages, and a
  `PlayoffMapping` spanning positions 1-4 to that cup's name
- WHEN it is cloned
- THEN the resulting `CupConfig` has `qualifiers = 4` and `hasThirdPlace = true`

#### Scenario: Two cups with different best-of formats

- GIVEN one cup bracket whose Final stage has `BestOf = 5` and another cup bracket whose Final
  has `BestOf = 1`
- WHEN the division is cloned
- THEN each resulting `CupConfig.bestOfByStage[Final]` matches its own source value exactly

### Requirement: Full Clone Reconstructs the Cross-Division Cup

For the division flagged `IsCrossDivisionCup`, the reverse-mapper MUST set `groupCount` to the
count of its Group-type stages, `qualifiersPerGroup` to the division's `QualifiersPerGroup`
field directly, and derive the pooled bracket's `CupConfig` (`hasThirdPlace`, `bestOfByStage`)
the same way as a regular division's cup, using `groupCount * qualifiersPerGroup` as the pooled
qualifier count (never from a `PlayoffMapping`, since the cross cup carries none).

#### Scenario: Cross-division cup with 3 groups pooling 2 qualifiers each

- GIVEN a cross-division cup division with 3 "Grupo N" stages and `QualifiersPerGroup = 2`
- WHEN it is cloned
- THEN the resulting `CrossCupConfig` has `enabled = true`, `groupCount = 3`,
  `qualifiersPerGroup = 2`, and its bracket matches a 6-team pooled shape

### Requirement: Full Clone Reconstructs Groupless (Playoffs-Only) Divisions

A division with zero non-cup Group-type stages MUST be reconstructed with `hasGroupStage =
false`, preserving its full cup/bracket configuration. It MUST NOT be dropped or misrepresented
as a group-only zone.

#### Scenario: Playoffs-only division clones with its bracket intact

- GIVEN a source division with no Group-type stage but one 8-qualifier cup bracket
- WHEN it is cloned
- THEN the resulting zone has `hasGroupStage = false` and a `CupConfig` with `qualifiers = 8`

### Requirement: Cloned Wizard Session Starts With Blank Dates

`StartDate` and `TeamRegistrationDeadline` MUST be blank in the pre-filled wizard session,
regardless of the source tournament's dates, and existing wizard validation (required,
deadline-before-start) MUST apply unchanged before submission is allowed.

#### Scenario: Submission blocked until dates are entered

- GIVEN a wizard session pre-filled from a clone
- WHEN the organizer attempts to submit without entering `StartDate` and
  `TeamRegistrationDeadline`
- THEN submission is rejected by the same validation a from-scratch wizard run would apply

### Requirement: Target Category Is an Explicit, Editable Organizer Choice

Cloning MUST require the organizer to choose the new tournament's `Category` as part of the
clone action. The wizard MAY default the category field to the source tournament's category as
a convenience, but it MUST remain an editable field the organizer can change before submitting;
it MUST NEVER be silently locked to the source's category.

#### Scenario: Organizer changes the category away from the source's

- GIVEN a clone of a Masculine tournament
- WHEN the organizer changes the pre-filled category to Feminine before submitting
- THEN the created tournament and all its divisions are Feminine, with no rejection tied to the
  source's original category

### Requirement: Organizer May Edit the Pre-Filled Structure Before Submitting

The pre-filled wizard session MUST support the same edit operations (add/edit/delete a zone,
edit cup config) as a from-scratch wizard run. The tournament actually created MUST reflect the
wizard state at submission time, not the original source structure.

#### Scenario: Deleting a zone before submit excludes it from the created tournament

- GIVEN a wizard session pre-filled from a source tournament with 3 divisions
- WHEN the organizer deletes one zone and then submits
- THEN the created tournament has 2 divisions, matching the edited state, not the original 3

### Requirement: No Instance Data Ever Carries Over

Cloning MUST NOT copy rosters, team registrations, matches, match series, standings, sanctions,
audit logs, or `DrawnAt` timestamps. Every division in the created tournament MUST start with
zero `DivisionTeamRegistration`s.

#### Scenario: Cloned tournament has empty rosters

- GIVEN a source tournament whose divisions have registered teams and played matches
- WHEN it is cloned and submitted
- THEN the created tournament's divisions have zero team registrations and zero matches

### Requirement: Ambiguous Source Structure Is Flagged, Never Silently Misrepresented

If a division's stages cannot be mapped to a single consistent wizard configuration — e.g. its
non-cup Group-type stages have differing `RoundRobinLegs`, a `BracketName`'s stage-type set does
not match any qualifier-count-derived shape, or a `PlayoffMapping`'s `Destination` has no
matching `BracketName` in that division's stages — the frontend MUST visibly flag that specific
zone or cup to the organizer instead of guessing a value. Flagging one zone MUST NOT prevent the
rest of the tournament's structure from pre-filling correctly, and the organizer MUST still be
able to edit the flagged zone manually before submitting.

#### Scenario: Mismatched sub-group round-robin legs is flagged

- GIVEN a source division whose sub-groups have inconsistent `RoundRobinLegs` values
- WHEN it is cloned
- THEN that zone is visibly flagged as needing review, no `roundRobinLegs` value is silently
  guessed, and the other cloned zones remain correctly pre-filled

#### Scenario: Orphaned playoff mapping is flagged

- GIVEN a source division with a `PlayoffMapping` whose `Destination` matches no `BracketName`
  among its stages
- WHEN it is cloned
- THEN that division's affected cup is flagged for review rather than assigned a guessed
  qualifier count

## Non-Goals

- A dedicated `POST /clone` deep-copy write path. Cloning always goes through the existing
  `/full` wizard-submission transaction.
- Auto-shifting dates by a year or any other heuristic. Dates are always blank after cloning.
- A selective checkbox picker for which divisions/cups to clone. Cloning is always full; the
  organizer edits/deletes unwanted zones inside the wizard.
- Restricting valid clone sources by season. Any tournament, including one in the same season,
  may be a source.
