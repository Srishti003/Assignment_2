# Project Assumptions & Architectural Decisions

This document details the assumptions, design tradeoffs, and architectural decisions made while developing the **Banking Ledger Service**.

---

## 1. Domain-Driven Design (DDD) Boundary
* **The Account Aggregate Root**:
  In financial ledger systems, updating an account balance without appending an immutable transaction record (or vice versa) results in ledger corruption. Therefore, the `Account` entity serves as the **Aggregate Root**.
  - All balance changes (`Credit` and `Debit`) MUST pass through the `Account` aggregate root.
  - Entities outside the domain cannot set the balance directly; internal setters are private.
  - The aggregate root guards all domain invariants (e.g. non-negative balance, amount > 0).

* **Transaction Immutability**:
  - Transactions in banking are append-only audit entries.
  - Once created, a transaction cannot be edited or deleted through the API. There are no `PUT`, `PATCH`, or `DELETE` endpoints for transactions.
  - Each transaction captures `BalanceAfter` (the account's balance immediately after the transaction completed), enabling point-in-time audit verification.

* **Account Opening Balance Consistency**:
  - The requirement states: *"The account balance must always represent the result of all valid transactions for that account."*
  - If a user opens an account with an initial balance (e.g. \$100), the system automatically creates an initial `Credit` transaction (`INIT-XXXXXX`). This ensures that `Balance == sum(Credits) - sum(Debits)` holds true from day one.

---

## 2. Currency, Precision & Scale
* **Data Type**: All monetary values are represented using .NET `decimal` (mapped to PostgreSQL `numeric(18, 2)`).
* **Scale**: Monetary values are constrained to 2 decimal places (standard currency scale). Rounding uses `MidpointRounding.AwayFromZero`.
* **Zero or Negative Amounts**: Transaction and transfer amounts must be strictly greater than zero (`amount > 0`).

---

## 3. Reference IDs & Idempotency
* **Client-Provided Reference ID**:
  - The `reference_id` parameter serves as an idempotency key (e.g., invoice number, checkout session ID, or client-generated UUID).
  - The system enforces a unique composite constraint on `(account_id, reference_id)`.
  - If a network failure occurs and the client retries the request with the same `reference_id`, the system detects the duplicate and returns `409 Conflict`, preventing double debits or double credits.
* **Transfer Reference ID**:
  - For money transfers between accounts, `reference_id` is unique across all transfers.

---

## 4. Concurrency & Atomicity
* **Pessimistic & Optimistic Concurrency**:
  - The `accounts` table includes a concurrency token (`RowVersion` / PostgreSQL `xmin`), which prevents the "lost update" problem if two simultaneous requests attempt to modify the same account balance.
  - Database-level check constraints (`CK_accounts_balance_non_negative`, `CK_transactions_amount_positive`) prevent negative balances even under high concurrency.
* **Atomicity**:
  - All multi-step mutations (e.g. posting a transaction and updating balance, or transferring money across two accounts) are wrapped in explicit database transactions (`IDbTransactionScope`).
  - If any failure occurs (e.g. insufficient funds on debit, destination account missing, or database crash), the transaction rolls back completely. Partial transfers are impossible.

---

## 5. Pagination & Sorting
* **Latest-First Sorting**:
  - All transaction queries are sorted in descending order by `created_at` (`ORDER BY "CreatedAt" DESC`).
  - A composite database index on `(account_id, created_at)` ensures $O(\log n)$ performance even with millions of ledger rows.
* **Pagination Parameters**:
  - `page`: 1-based page index (defaults to `1`, must be $\ge 1$).
  - `limit`: number of items per page (defaults to `10`, maximum capped at `100` to prevent denial-of-service query spikes).
  - Responses include `currentPage`, `pageSize`, `totalRecords`, `totalPages`, and `items`.

---

## 6. Error Handling & HTTP Status Codes
* **RFC 7807 Problem Details**:
  All error responses adhere to RFC 7807 (`application/problem+json`).
* **HTTP Status Code Mapping**:
  - `400 Bad Request`: Validation failure, invalid request body, or invalid self-transfer.
  - `404 Not Found`: Account ID does not exist.
  - `409 Conflict`: Duplicate `reference_id` detected.
  - `422 Unprocessable Entity`: Insufficient account balance for debit or transfer.
  - `500 Internal Server Error`: Unhandled server exception (logged with stack trace; sanitized for client).
