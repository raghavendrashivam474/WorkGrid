# ADR-0004: WorkGrid Local Identity Architecture

## Context
WorkGrid v2.0 operated anonymously with all CRUD and workflow operations accessible to any user without identification. Phase 3 mandates authentication and identity control without introducing remote backend dependencies (maintaining local-first architecture).

## Decision
1. **Separation of Concepts**: `Employee` represents personnel receiving physical assets; `User` represents an operator authenticating to use WorkGrid.
2. **Local Credential Storage**: Passwords are never stored in plaintext. Hashing is performed using `PBKDF2` (`Rfc2898DeriveBytes`) with cryptographically secure per-user random salt (128 bits) and 100,000 iterations using SHA256.
3. **Session Management**: Pure in-memory session managed by `ISessionService` without JWTs or OAuth tokens. Session is cleared on logout or application termination.
4. **First-Run Bootstrap**: On initial application launch with zero users, WorkGrid prompts the operator to register the primary Administrator account. Subsequent registrations require administrative authentication.

## Consequences
### Positive
- Strict local credential security without plaintext leakage.
- Offline and local-first architecture preserved completely.
- Intuitive initial bootstrap flow for fresh installations.

### Negative / Operational Considerations
- Sessions do not persist across app restarts (intentional security posture).
- If admin credentials are lost on an unbacked device, standard SQLite admin reset or reinstall is required.
