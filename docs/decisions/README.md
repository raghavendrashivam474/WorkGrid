# WorkGrid — Architectural Decision Records (ADR)

This directory contains records of significant architectural decisions made during the design and evolution of the WorkGrid platform.

---

## 📋 When to Write an ADR

An ADR must be created when making a consequential architectural choice, such as:
* Changing foundational framework dependencies
* Altering the agreed layer boundary or dependency direction
* Introducing major structural paradigms (e.g., sync protocol design, storage engine shifts)

Routine implementation details and minor refactorings do not require an ADR.

---

## 📝 ADR Template Format

Each ADR should follow this standard structure:

```markdown
# ADR-XXXX: [Title]

* **Status:** [Proposed | Accepted | Superseded | Deprecated]
* **Date:** YYYY-MM-DD
* **Deciders:** [Names/Roles]

## Context
[What problem or situation prompted this decision?]

## Decision
[What architectural change or approach is being adopted?]

## Reason & Motivation
[Why was this option chosen over others?]

## Alternatives Considered
* **Alternative 1:** [Why it was rejected]
* **Alternative 2:** [Why it was rejected]

## Consequences
* **Positive:** [What becomes easier or better]
* **Negative / Trade-offs:** [What overhead or constraint is introduced]
🗂️ Decision Log

| ID | Date | Title | Status |
| --- | --- | --- | --- |
| **—** | — | No decisions recorded yet. Baseline architecture defined in S0.1. | — |
