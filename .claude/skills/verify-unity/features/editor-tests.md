# Editor tests

The project's EditMode and PlayMode tests run inside the open Editor, so verification never needs to close it.

## Sub-features

- `tests-editor` runs EditMode tests with an optional name filter.
- `tests-playmode` runs PlayMode tests with an optional name filter.

## How to get to it (user POV)

- Test Runner window, here `vu.sh test`.

## Driving it with vu.sh

Preconditions:

- `vu.sh doctor` prints `OK` and play mode is stopped.

- **EditMode.** Run `$V test editor <filter>`. The JSON has `"success":true` and `Summary.Failed` is 0. The JSON is saved as `test-editor.json` in the evidence folder.
- **PlayMode.** Run `$V test playmode <filter>`. Same checks, saved as `test-playmode.json`.
- **Red check.** For a new test, break the behavior it covers once and rerun. `Summary.Failed` is 1 or more. Restore it.

## Gotchas

- `Summary.Total` 0 with `success` true means the filter matched nothing. Treat it as a failure when tests were expected.
- `unity test` (batchmode) fails while the Editor is open. Use `$V test`.
- PlayMode tests also need the background override. `vu.sh test` does not set it, so run `$V doctor` first, which does.
