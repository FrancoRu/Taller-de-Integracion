# Admin Data Administration Panel Specification

## Purpose

Define the "Administración de datos" Admin view — replacing the existing "Test" tab/panel — with
two cards: a primary "Base de datos" card (backup generation, deletion, restore, and the backups
table) and a secondary "Test" card (relocated seed action). All destructive actions require
confirmation.

## Requirements

### Requirement: Panel Renamed and Restructured Into Two Cards

The system MUST rename the existing Admin "Test" tab/panel to "Administración de datos" and
present exactly two cards: "Base de datos" (primary) and "Test" (secondary, below it).

#### Scenario: Admin opens the renamed panel

- GIVEN an authenticated Admin navigates to the former "Test" route
- WHEN the page renders
- THEN the tab/page title reads "Administración de datos"
- AND two cards are shown: "Base de datos" above "Test"

### Requirement: Base de Datos Card Exposes Wipe and Backup-Generation Actions

The "Base de datos" card MUST include a "Borrar los datos" button (the existing wipe action,
relabeled, behavior unchanged) and a "Generar respaldo" button that triggers an on-demand backup.

#### Scenario: Generar respaldo triggers a manual backup

- GIVEN the Admin is on the "Administración de datos" panel
- WHEN they click "Generar respaldo"
- THEN a manual backup is triggered and, on success, appears in the backups table

### Requirement: Backups Table Columns and Row Actions

The "Base de datos" card MUST contain a table with exactly the columns "Fecha", "Peso", "Forma de
creación", "Actions", sourced from the backup catalog. The "Actions" column MUST show a trash icon
(delete) and a restore icon, following the project's existing `TableRowActions` icon pattern.

#### Scenario: Table shows catalog entries with correct columns

- GIVEN the backup catalog has at least one record
- WHEN the panel loads
- THEN the table displays "Fecha", "Peso", "Forma de creación", and "Actions" for each row

#### Scenario: Actions column offers delete and restore

- GIVEN a row in the backups table
- WHEN the "Actions" cell is inspected
- THEN it shows a trash icon and a restore icon

### Requirement: Confirmation Required for Delete and Restore

Both the delete and restore row actions MUST show a confirmation modal (via the project's
`confirmDialog.ts` / SweetAlert2 `confirmDelete` / `confirmAction` pattern) before executing, and
MUST NOT execute if the Admin cancels.

#### Scenario: Delete requires confirmation

- GIVEN the Admin clicks the trash icon on a backup row
- WHEN the confirmation modal appears and the Admin cancels
- THEN the backup is not deleted

#### Scenario: Restore requires confirmation

- GIVEN the Admin clicks the restore icon on a backup row
- WHEN the confirmation modal appears and the Admin confirms
- THEN the restore flow is triggered for that backup

### Requirement: Test Card Retains Unchanged Seed Behavior

The "Test" card MUST contain the "Cargar Datos de prueba" button, relocated from the former
top-level panel, with behavior unchanged from before this change.

#### Scenario: Seed action still works from the Test card

- GIVEN the Admin is on the "Administración de datos" panel
- WHEN they click "Cargar Datos de prueba" in the "Test" card
- THEN test data is seeded exactly as it was before the panel rename

### Requirement: Panel and Its Actions Are Admin-Only

The system MUST restrict access to the "Administración de datos" panel and all its actions
(wipe, backup, delete, restore, seed) to the `Admin` role.

#### Scenario: Non-Admin cannot access the panel

- GIVEN an authenticated non-Admin user
- WHEN they attempt to navigate to "Administración de datos"
- THEN access is denied
