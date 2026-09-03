# Statistics Filter UX Specification

## Purpose

Define how `/panel/estadisticas` (`StatisticsPage`) presents its scope
filters and how it reloads when the scope changes.

Scope: `StatisticsPage` under Vitest + Testing Library with mocked module
hooks.

## Requirements

### Requirement: Scope Selects Show Their Default Option

The "Temporada" and "Torneo" selects MUST display their default option
("Todas" / "Todos") whenever no specific value is selected, not an empty
field with only the floating label visible.

#### Scenario: Empty selects read as "Todas" / "Todos"

- GIVEN the statistics page has loaded and no filter has been touched
- WHEN the "Temporada" and "Torneo" comboboxes are inspected
- THEN the "Temporada" combobox shows "Todas"
- AND the "Torneo" combobox shows "Todos"

### Requirement: A Scope Change Refreshes Only the Statistics Content

Changing the season or tournament filter MUST NOT unmount the page shell or
the filter bar. The filter controls MUST stay mounted and interactive while
the statistics reload.

#### Scenario: Filter bar survives a refilter

- GIVEN the statistics page has loaded and the summary counts are shown
- WHEN a tournament is selected and its summary fetch has not yet resolved
- THEN the "Temporada" and "Torneo" comboboxes are still in the document
- AND the page is not showing only the initial-load skeleton

#### Scenario: Initial load still shows the skeleton

- GIVEN the statistics page is mounted and the first summary fetch has not
  resolved
- WHEN the page renders
- THEN the card skeleton is shown in the content area
- AND the filter bar is already mounted
