# SPT-FreeSpace — Feasibility Study and Codex Handoff

Target: **SPT 4.0.13 / EFT 0.16.9.0.40087**

This package contains the implementation brief for a small, client-only BepInEx mod that renders an `available/total` storage-capacity overlay on player-owned grid containers.

Read in this order:

1. `FEASIBILITY.md` — feasibility verdict, semantics, architecture, risks, and scope.
2. `CODEX_HANDOFF.md` — binding implementation requirements and milestone plan.
3. `SOURCE_NOTES.md` — source-backed API leads that must be verified against the exact local SPT 4.0.13 source tree.
4. `MANUAL_TEST_MATRIX.md` — runtime validation matrix.
5. `CODEX_PROMPT.txt` — compact bootstrap prompt for a fresh Codex session.

The handoff deliberately avoids inventing obfuscated method names. Codex must map every runtime hook against the user's exact matching source tree before implementation.
