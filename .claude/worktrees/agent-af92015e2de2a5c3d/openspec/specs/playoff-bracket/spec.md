# Playoff Bracket Specification

## Purpose

Render elimination-stage matches (cuartos, semifinal, tercer puesto, final) of a tournament division as a readable bracket tree on the public tournament view, derived client-side from existing Stage/Match data, with graceful degradation when advancement cannot be reliably inferred.

## Requirements

### Requirement: Llaves Tab on Public Tournament Page

The system MUST expose a new "Llaves" tab on `PublicTournamentPage`, in addition to (not replacing) the existing "Partidos" flat match-list tab.

#### Scenario: Both tabs available

- GIVEN a visitor opens a tournament's public page for a division with elimination stages
- WHEN the page renders its tabs
- THEN both "Partidos" (existing flat match list) and "Llaves" (new bracket) tabs are visible and selectable
- AND selecting "Llaves" does not remove or alter the "Partidos" tab's content

#### Scenario: No elimination stages for the division

- GIVEN a division has no elimination-stage matches
- WHEN the visitor opens "Llaves"
- THEN the tab MUST show an empty state message instead of an empty or broken tree

### Requirement: Bracket Scoped Per Division

The system MUST render one independent bracket tree per division; brackets from different divisions MUST NOT merge or cross-link.

#### Scenario: Multi-division tournament

- GIVEN a tournament has two or more divisions, each with elimination stages
- WHEN the visitor views "Llaves" for a division
- THEN only that division's matches appear in the tree

### Requirement: Round Grouping by Stage Type Order

The system MUST group elimination matches into rounds using the canonical `StageType` sequence (e.g., Cuartos, Semifinal, Final) and MUST group matches within a round by `stageId`.

#### Scenario: Standard bracket depth

- GIVEN a division has Cuartos, Semifinal, and Final stages with matches
- WHEN the bracket renders
- THEN columns appear left-to-right in Cuartos, Semifinal, Final order, each containing only matches from its own `stageId`

### Requirement: Third Place as Side Match, Final as Terminal Node

The system MUST render `ThirdPlace` stage matches as a side match visually separated from the main advancement path, and MUST render the `Final` stage match as the tree's terminal (rightmost) node.

#### Scenario: Third place and final coexist

- GIVEN a division has both `Final` and `ThirdPlace` stages
- WHEN the bracket renders
- THEN the Final match is the rightmost node on the main path
- AND the ThirdPlace match renders alongside it as a clearly separate side match, not chained into the main advancement line

### Requirement: TBD Slots for Unresolved Participants

The system MUST render a "TBD" placeholder for any bracket slot whose participant is not yet determined (later-round match not yet populated with a team).

#### Scenario: Next round not yet seeded

- GIVEN a Semifinal match has not been played
- WHEN the Final round is rendered
- THEN the Final slot depending on that Semifinal's winner shows "TBD" instead of a blank or incorrect team

### Requirement: Match Node Content

Each bracket match node MUST display both participating teams (or TBD) and, when available, the recorded score.

#### Scenario: Played match with score

- GIVEN a match has both teams and a recorded result
- WHEN its node renders
- THEN both team names and the score are visible on the node

### Requirement: Client-Side Connector Inference

The system MUST attempt to draw a connector line from a match to its next-round slot by matching that match's `winningTeamId` to a participant team of a match in the immediately following round, using only client-side data (no backend linkage field required).

#### Scenario: Clear winner advances

- GIVEN a Cuartos match has a `winningTeamId` that appears as a team in exactly one Semifinal match
- WHEN the bracket renders
- THEN a connector line is drawn from the Cuartos match to that Semifinal match

### Requirement: Graceful Degradation on Ambiguous Inference

The system MUST NOT render a connector line when advancement cannot be unambiguously inferred — including an unplayed match (no `winningTeamId` yet), or a data tie/ambiguity where the same team could plausibly map to more than one next-round slot. In these cases the system MUST still render a clean column layout without a possibly-incorrect connector.

#### Scenario: Unplayed match, no winner yet

- GIVEN a Cuartos match has no `winningTeamId` set
- WHEN the bracket renders
- THEN no connector line is drawn from that match
- AND the match node and the following round's column still render in their correct positions

#### Scenario: Ambiguous winner mapping

- GIVEN a match's `winningTeamId` matches teams in more than one match of the next round, or does not uniquely resolve
- WHEN the bracket renders
- THEN the system MUST render the column layout without drawing a connector for that match rather than guessing which slot it advances to
