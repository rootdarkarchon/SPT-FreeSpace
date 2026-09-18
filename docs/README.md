# SPT-FreeSpace documentation

Target: **SPT 4.1.x / EFT 0.16.9.40743**. Automated baseline: **SPT 4.1.6**.

FreeSpace renders recursive capacity on player-owned grid-container tiles,
using `used/total` by default or configurable `available/total`.

Read in this order:

1. `STATUS.md` — current migration validation and historical results.
2. `SOURCE_NOTES.md` — verified 4.1 API mapping, followed by historical 4.0 evidence.
3. `MANUAL_TEST_MATRIX.md` — runtime validation matrix and pending acceptance checks.
4. `FEASIBILITY.md` and `CODEX_HANDOFF.md` — historical design and original 4.0 brief;
   superseded where current implementation or migration documentation differs.

Use the exact target sources, supplied mapping, and installed metadata when
checking compatibility; do not infer live behavior from a successful build.
