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

## 📑 ADR Index

| ADR | Title | Status | Date |
| --- | --- | --- | --- |
| [ADR-0001](ADR-0001-local-persistence-efcore-sqlite.md) | Local Persistence with EF Core and SQLite | Accepted | Phase 1 |
| [ADR-0002](ADR-0002-entity-deletion-guard-rules.md) | Active Assignment Guard Rules for Entity Deletion | Accepted | Phase 1 |
