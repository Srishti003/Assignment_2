# Banking Ledger API Documentation

Welcome to the **Banking Ledger API** documentation. This service is built following **Domain-Driven Design (DDD)** and **Clean Architecture** principles using **C# .NET 10**, **ASP.NET Core**, and **PostgreSQL**.

When the application is running, an interactive Swagger UI is available at:
`http://localhost:5000` (or `http://localhost:5000/index.html`)

---

## Base URLs
- **Docker Compose (Default):** `http://localhost:5000`
- **Local .NET CLI (dotnet run):** `http://localhost:5000` or `https://localhost:5001`
- **Base Route Prefix:** `/api/v1`

---

## Standard Error Response Format (RFC 7807)
All error responses return standard `application/problem+json` envelopes:

```json
{
  "type": "about:blank",
  "title": "Insufficient Balance",
  "status": 422,
  "detail": "Account 3fa85f64-5717-4562-b3fc-2c963f66afa6 has insufficient funds. Current balance: $50.00, Requested amount: $100.00",
  "instance": "/api/v1/accounts/3fa85f64-5717-4562-b3fc-2c963f66afa6/transactions",
  "currentBalance": 50.00,
  "requestedAmount": 100.00
}
```

---

## Endpoints Summary

| Method | Endpoint | Description | Status Codes |
|---|---|---|---|
| `POST` | `/api/v1/accounts` | Create / Open a new account | `201 Created`, `400 Bad Request` |
| `GET` | `/api/v1/accounts/{id}` | Get account details & balance | `200 OK`, `404 Not Found` |
| `GET` | `/api/v1/accounts/{id}/balance` | Get account current balance | `200 OK`, `404 Not Found` |
| `GET` | `/api/v1/accounts` | List all registered accounts | `200 OK` |
| `POST` | `/api/v1/accounts/{accountId}/transactions` | Post Credit / Debit transaction | `201 Created`, `400`, `404`, `409`, `422` |
| `GET` | `/api/v1/accounts/{accountId}/transactions` | Query paginated transactions (latest-first) | `200 OK`, `400 Bad Request`, `404 Not Found` |
| `POST` | `/api/v1/transfers` | Transfer money atomically between accounts | `200 OK`, `400`, `404`, `409`, `422` |
| `GET` | `/health` | Service health status check | `200 OK` |

---

## Detailed Endpoint Specifications

### 1. Create Account
Opens a new account. An optional initial deposit can be supplied; if provided, an opening Credit transaction is generated automatically to ensure 100% balance consistency with transaction history.

- **URL:** `POST /api/v1/accounts`
- **Request Body:**
```json
{
  "accountName": "Alice Johnson",
  "initialBalance": 100.00
}
```
- **Response `201 Created`:**
```json
{
  "id": "e0b965c4-0822-4467-bc1a-637dc4130091",
  "name": "Alice Johnson",
  "balance": 100.00,
  "createdAt": "2026-10-31T02:00:00Z",
  "updatedAt": "2026-10-31T02:00:00Z"
}
```

---

### 2. Get Account by ID
Retrieves account details and the current balance.

- **URL:** `GET /api/v1/accounts/{id}`
- **Response `200 OK`:**
```json
{
  "id": "e0b965c4-0822-4467-bc1a-637dc4130091",
  "name": "Alice Johnson",
  "balance": 100.00,
  "createdAt": "2026-10-31T02:00:00Z",
  "updatedAt": "2026-10-31T02:00:00Z"
}
```
- **Response `404 Not Found`:**
```json
{
  "title": "Account Not Found",
  "status": 404,
  "detail": "Account with ID 'e0b965c4-0822-4467-bc1a-637dc4130091' was not found."
}
```

---

### 3. Get Account Balance
Quick balance inquiry for an account.

- **URL:** `GET /api/v1/accounts/{id}/balance`
- **Response `200 OK`:**
```json
{
  "accountId": "e0b965c4-0822-4467-bc1a-637dc4130091",
  "balance": 100.00,
  "timestamp": "2026-10-31T02:05:00Z"
}
```

---

### 4. Create Transaction (Credit or Debit)
Applies a credit (deposit) or debit (withdrawal) against an existing account.
- **Transaction Immutability:** Once created, a transaction cannot be edited or deleted via API.
- **Atomicity:** The balance update and transaction row insertion succeed or fail together as one unit.
- **Idempotency:** Re-submitting a duplicate `reference_id` for the same account is rejected with `409 Conflict`.
- **Overdraft Protection:** Debits with insufficient balance are rejected with `422 Unprocessable Entity`.

- **URL:** `POST /api/v1/accounts/{accountId}/transactions`
- **Request Body (Credit):**
```json
{
  "type": "credit",
  "amount": 50.00,
  "referenceId": "INV-2026-001",
  "description": "Invoice payment"
}
```
- **Request Body (Debit):**
```json
{
  "type": "debit",
  "amount": 30.00,
  "referenceId": "WDL-2026-002",
  "description": "Grocery store purchase"
}
```
- **Response `201 Created`:**
```json
{
  "id": "76495b68-e568-450f-a36c-dfc581907cb5",
  "accountId": "e0b965c4-0822-4467-bc1a-637dc4130091",
  "type": "credit",
  "amount": 50.00,
  "referenceId": "INV-2026-001",
  "description": "Invoice payment",
  "balanceAfter": 150.00,
  "createdAt": "2026-10-31T02:10:00Z"
}
```

---

### 5. Get Paginated Transactions (Latest-First)
Queries the transaction ledger for an account with pagination and latest-first sorting.

- **URL:** `GET /api/v1/accounts/{accountId}/transactions?page=1&limit=10`
- **Query Parameters:**
  - `page` (integer, optional, default: `1`): The 1-based page index.
  - `limit` (integer, optional, default: `10`, max: `100`): The page size.
- **Sorting Rule:** Always sorted strictly latest-first based on `created_at` (descending).
- **Response `200 OK`:**
```json
{
  "currentPage": 1,
  "pageSize": 10,
  "totalRecords": 4,
  "totalPages": 1,
  "items": [
    {
      "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      "accountId": "e0b965c4-0822-4467-bc1a-637dc4130091",
      "type": "debit",
      "amount": 20.00,
      "referenceId": "REF-4",
      "description": "Debit 20",
      "balanceAfter": 100.00,
      "createdAt": "2026-10-31T02:15:00Z"
    },
    {
      "id": "b2c3d4e5-f6a7-8901-bcde-f12345678901",
      "accountId": "e0b965c4-0822-4467-bc1a-637dc4130091",
      "type": "debit",
      "amount": 30.00,
      "referenceId": "REF-3",
      "description": "Debit 30",
      "balanceAfter": 120.00,
      "createdAt": "2026-10-31T02:12:00Z"
    },
    {
      "id": "c3d4e5f6-a7b8-9012-cdef-123456789012",
      "accountId": "e0b965c4-0822-4467-bc1a-637dc4130091",
      "type": "credit",
      "amount": 50.00,
      "referenceId": "REF-2",
      "description": "Credit 50",
      "balanceAfter": 150.00,
      "createdAt": "2026-10-31T02:08:00Z"
    },
    {
      "id": "d4e5f6a7-b8c9-0123-def1-234567890123",
      "accountId": "e0b965c4-0822-4467-bc1a-637dc4130091",
      "type": "credit",
      "amount": 100.00,
      "referenceId": "REF-1",
      "description": "Credit 100",
      "balanceAfter": 100.00,
      "createdAt": "2026-10-31T02:01:00Z"
    }
  ]
}
```

---

### 6. Bonus: Money Transfer API
Transfers money atomically between two distinct accounts.

- **URL:** `POST /api/v1/transfers`
- **Request Body:**
```json
{
  "sourceAccountId": "e0b965c4-0822-4467-bc1a-637dc4130091",
  "destinationAccountId": "f1a2b3c4-d5e6-7890-abcd-ef0987654321",
  "amount": 30.00,
  "referenceId": "TRF-2026-0001",
  "description": "Payment for shared groceries"
}
```
- **Response `200 OK`:**
```json
{
  "id": "99aa88bb-77cc-66dd-55ee-44ff33221100",
  "sourceAccountId": "e0b965c4-0822-4467-bc1a-637dc4130091",
  "destinationAccountId": "f1a2b3c4-d5e6-7890-abcd-ef0987654321",
  "amount": 30.00,
  "referenceId": "TRF-2026-0001",
  "description": "Payment for shared groceries",
  "status": "completed",
  "sourceTransactionId": "11223344-5566-7788-9900-aabbccddeeff",
  "destinationTransactionId": "aabbccdd-eeff-0011-2233-445566778899",
  "sourceAccountNewBalance": 70.00,
  "destinationAccountNewBalance": 80.00,
  "createdAt": "2026-10-31T02:20:00Z"
}
```
- **Failure Behaviors:**
  - If source account has insufficient balance -> `422 Unprocessable Entity`.
  - If either account does not exist -> `404 Not Found`.
  - If source and destination are the same account -> `400 Bad Request`.
  - If `referenceId` was already processed -> `409 Conflict`.
  - Any error rolls back both accounts; partial transfer is impossible.
