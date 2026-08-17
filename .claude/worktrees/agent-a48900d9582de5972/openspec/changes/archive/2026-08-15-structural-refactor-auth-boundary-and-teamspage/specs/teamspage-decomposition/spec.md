# TeamsPage Decomposition Specification

## Purpose

`TeamsPage.tsx` (602 lines) currently combines data fetching, debounced filter state, pagination, create/edit dialog forms, and delete confirmation in a single component with no container/presentational split. This spec locks the observable behavior of every user-facing flow so the decomposition (container owning state/handlers + presentational children for filter bar, data grid, create dialog, edit dialog) is provably behavior-preserving. No visual, UX, or API-contract change is permitted.

## Requirements

### Requirement: Structural Split

The team list view MUST be composed of a container component owning data fetching, filter/pagination state, and submit/delete handlers, plus stateless presentational child components (filter bar, data grid, create dialog, edit dialog) that receive state and callbacks via props.

#### Scenario: Container delegates rendering to presentational children

- GIVEN the decomposed `TeamsPage` module
- WHEN its component tree is inspected
- THEN a container component owns `useTeam`, filter/pagination state, and dialog open/submit handlers
- AND the filter bar, data grid, create dialog, and edit dialog are separate components receiving only props (no direct `useTeam` calls)

### Requirement: Filtering Preserves Debounce and Query Behavior

Typing into the name, code, or shirt-color filter fields MUST update the input immediately, MUST debounce the outbound query by 1000ms of inactivity, and MUST reset pagination to page 0 on filter change, exactly as before decomposition.

#### Scenario: Debounced filter triggers a new fetch

- GIVEN the team list is displayed with no active filters
- WHEN the user types a value into the "Nombre" field and stops typing
- THEN the input reflects the typed value immediately
- AND `getTeamsByFiltered` is called with the typed value only after 1000ms of inactivity
- AND the pagination page resets to 0

### Requirement: Pagination Preserves Current Page and Size

Changing the DataGrid's page or page size MUST refetch teams for the requested page/size, and repeated identical pagination changes MUST NOT trigger redundant state updates.

#### Scenario: Changing page fetches the next page

- GIVEN a team list with more rows than one page
- WHEN the user navigates to page 2 in the DataGrid
- THEN `getTeamsByFiltered` is called with `pageNumber: 2` and the current `pageSize`
- AND the grid displays a loading state while the fetch is in flight

### Requirement: Create Dialog Preserves Validation and Submit Flow

Opening the create dialog MUST reset the form; submitting MUST validate that name, code, and logo are present before calling `addTeam`, close the dialog, reset the form, refetch the list, and show a success alert on success.

#### Scenario: Create dialog opens with an empty form

- GIVEN the team list is displayed
- WHEN the user clicks the "new team" action
- THEN the create dialog opens with empty name, code, shirt-color, and logo fields

#### Scenario: Submitting create without a logo is blocked

- GIVEN the create dialog is open with name and code filled in but no logo selected
- WHEN the user submits the create form
- THEN a warning alert is shown requiring a logo
- AND `addTeam` is not called

#### Scenario: Successful create closes dialog and refreshes list

- GIVEN the create dialog is open with name, code, and logo filled in
- WHEN the user submits the create form and the request succeeds
- THEN `addTeam` is called with the trimmed form values
- AND the dialog closes, the form resets, the team list refetches, and a success alert is shown

### Requirement: Edit Dialog Preserves Prefill and Submit Flow

Clicking edit on a row MUST prefill the form with that team's current values (excluding logo); submitting MUST validate name and code are present before calling `putTeamById`, close the dialog, refetch the list, and show a success alert on success.

#### Scenario: Edit dialog opens prefilled with the selected team

- GIVEN a team row with name "River", code "RIV", and color "Rojo"
- WHEN the user clicks the "edit" action on that row
- THEN the edit dialog opens with name "River", code "RIV", and shirt-color "Rojo" prefilled

#### Scenario: Successful edit closes dialog and refreshes list

- GIVEN the edit dialog is open for a team with valid name and code
- WHEN the user submits the edit form and the request succeeds
- THEN `putTeamById` is called with the team id and the trimmed form values
- AND the dialog closes, the form resets, the team list refetches, and a success alert is shown

### Requirement: Delete Confirmation Flow Preserved

Clicking delete on a row MUST show a confirmation dialog before calling `deleteTeamById`; declining MUST NOT call `deleteTeamById`; confirming MUST call `deleteTeamById` and show a success alert.

#### Scenario: Declining the confirmation cancels the delete

- GIVEN the user clicks the "delete" action on a team row
- WHEN the confirmation dialog is shown and the user declines
- THEN `deleteTeamById` is not called

#### Scenario: Confirming deletes the team and shows success

- GIVEN the user clicks the "delete" action on a team row
- WHEN the confirmation dialog is shown and the user confirms
- THEN `deleteTeamById` is called with that row's id
- AND a success alert is shown

## Non-Goals

- No new features, visual changes, or UX changes to the team list, filters, or dialogs.
- No changes to other `views/*Page.tsx` files.
- No changes to the `useTeam` hook or team module API/service layer.
