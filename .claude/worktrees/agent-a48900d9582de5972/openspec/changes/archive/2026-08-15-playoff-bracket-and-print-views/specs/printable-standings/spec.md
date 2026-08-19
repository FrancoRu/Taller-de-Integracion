# Printable Standings Specification

## Purpose

Give organizers a clean, native-browser-printable output of a division's standings and/or top scorers (goleadores), without introducing a PDF library or new dependency.

## Requirements

### Requirement: Print Action on Division Standings View

The system MUST provide a visible "Imprimir" (print) action on the `divisionStandings` view that triggers the browser's native print flow (`window.print()`).

#### Scenario: Organizer triggers print

- GIVEN an organizer is viewing a division's standings page
- WHEN they activate the "Imprimir" action
- THEN the browser's native print dialog opens via `window.print()`
- AND no PDF-generation library or new package is invoked

### Requirement: Selectable Print Target (Standings or Goleadores)

The system MUST let the user select which table to print — standings, goleadores, or both — before invoking print, producing one printable sheet reflecting that selection.

#### Scenario: Print standings only

- GIVEN the organizer selects "Standings" as the print target
- WHEN they trigger print
- THEN the printed sheet contains only the standings table

#### Scenario: Print goleadores only

- GIVEN the organizer selects "Goleadores" as the print target
- WHEN they trigger print
- THEN the printed sheet contains only the top-scorers table

#### Scenario: Print both tables

- GIVEN the organizer selects both "Standings" and "Goleadores" as print targets
- WHEN they trigger print
- THEN the printed sheet contains both tables, standings followed by goleadores

### Requirement: Print-Only CSS Hides App Chrome

The system MUST use `@media print` rules to hide non-table application chrome (navigation, tabs, buttons, headers/footers not part of the printable content) so only the selected table(s) and minimal identifying context (e.g., division/tournament name) appear on the printed output.

#### Scenario: Chrome hidden when printing

- GIVEN the organizer opens the print dialog from the standings view
- WHEN the print preview renders
- THEN navigation, action buttons, and unrelated page chrome are not visible in the preview
- AND only the selected table(s) plus minimal identifying header text are visible

### Requirement: Page-Break Handling for Long Tables

The system MUST apply print CSS rules that avoid splitting a single table row across a page break and that repeat the table header on each printed page for tables spanning multiple pages.

#### Scenario: Long standings table spans multiple pages

- GIVEN a division's standings table has enough rows to exceed one printed page
- WHEN the sheet is printed or previewed
- THEN no row is split across two pages
- AND the table header repeats at the top of each subsequent printed page

### Requirement: No New Dependency for Printing

The system MUST implement printing using only native browser APIs (`@media print` CSS and `window.print()`); it MUST NOT introduce a PDF-generation or print-rendering library.

#### Scenario: Dependency check

- GIVEN the printable-standings feature is implemented
- WHEN the project's dependency manifest is inspected
- THEN no new PDF or print-rendering package has been added
