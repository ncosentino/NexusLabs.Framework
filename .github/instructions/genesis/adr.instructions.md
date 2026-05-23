---
applyTo: "docs/adr/**"
---

# Architecture Decision Records

## Location and naming

- ADRs live in `docs/adr/` at the repository root.
- File naming: `adr-NNNN-title-slug.md` with sequential 4-digit numbering.
- Title slugs: lowercase, hyphens, no special characters, 3-5 words.

## Status lifecycle

| Status | Meaning |
|--------|---------|
| Proposed | Decision documented, awaiting review or acceptance |
| Accepted | Decision approved and in effect |
| Rejected | Decision was considered but not adopted |
| Superseded | Replaced by a newer ADR (set `superseded_by` field) |
| Deprecated | No longer relevant due to changed circumstances |

When superseding an existing ADR, update the old ADR's `superseded_by` front matter
field to reference the new ADR, and set the old ADR's status to `Superseded`.

## Content quality

### Write as a permanent document

ADRs are long-lived reference documents. Do not include:

- Planning session context ("while discussing Phase 1.2B...")
- WIP or temporal jargon ("in the previous plan we decided...")
- Sprint/iteration references ("during Sprint 14...")
- Conversation history ("the user mentioned that...")

If a specific event motivated the decision, reference it by date and substance:

- ✓ "On 2026-04-07, a production outage caused by unbounded cache growth revealed the need for an eviction policy."
- ✗ "While fixing the cache issue from last week's incident, we decided..."

### Evidence and objectivity

- Ground claims in codebase evidence — reference specific files, packages, or patterns.
- Present facts and reasoning, not opinions.
- Document both benefits and drawbacks honestly.
- Distinguish between verified facts and assumptions.

## Working with existing ADRs

- Before creating a new ADR, check if the decision is already documented.
- When modifying code that contradicts an existing accepted ADR, flag the
  contradiction — do not silently diverge from a documented decision.
- When a new decision renders an existing ADR obsolete, create the new ADR
  and update the old one rather than editing the old ADR in place.
