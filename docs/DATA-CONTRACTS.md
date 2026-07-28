# Data and API contracts

## Operation IDs and safe retries

Clients generate a UUID operation ID before sending a stock-changing request and
retain it while retrying an uncertain response.

- Same operation ID plus equivalent material payload: return the original
  result and do not change stock again.
- Same operation ID plus different material payload: `409 Conflict`.
- A new business action requires a new operation ID.

The database key is `(OperationId, ProductId)`. A work-order issue batch uses one
operation ID across its product lines, so every line remains independently
unique while the application verifies the batch as a whole.

This is database-scoped retry safety, not a distributed exactly-once guarantee.
A racing retry may observe a uniqueness conflict before the winning transaction
is visible; the caller may retry the same unchanged request.

## Product contract

Product creation accepts `openingStock`. When it is greater than zero, the API
creates the product and one `OpeningBalance` movement in the same commit.

Product metadata updates require the latest `version` returned by the read API.
`currentStock` is deliberately absent from the update contract. Unknown JSON
members are rejected, so a client cannot smuggle a balance edit through product
metadata.

## Direct stock movement contract

```json
{
  "operationId": "54e6f548-ad1d-4852-ae77-0dbd2372891d",
  "productId": "a product UUID",
  "type": "Receipt",
  "quantity": 5,
  "reason": "Supplier receipt",
  "reference": "PO-100"
}
```

| Type | Quantity input | Balance effect | Location snapshot |
|---|---:|---:|---|
| Receipt | positive | `+quantity` | destination is the product's recorded location |
| Return | positive | `+quantity` | destination is the product's recorded location |
| Issue | positive | `-quantity` | source is the product's recorded location |
| Adjustment | non-zero signed | `+quantity` | source and destination both snapshot the product's recorded location |
| OpeningBalance | not accepted directly | `+quantity` | created only with a new product |
| Transfer | not accepted for new writes | none | historical rows may remain queryable only |

Every successful movement stores `BalanceBefore`, `BalanceAfter`, actor, reason,
reference, timestamp, and unit cost at posting time. These are historical
snapshots. Persisted movement rows are append-only.

## Work-order issue batch

```json
{
  "operationId": "698a4e11-ab15-4426-ac00-f9184f380e7d",
  "items": [
    {
      "productId": "a product UUID",
      "quantity": 2,
      "notes": "Picked for pump repair"
    }
  ]
}
```

A product may appear once per work order and once per issue batch. The entire
batch is prevalidated before any balance changes. Every quantity must be
positive, no line may exceed remaining requested demand, and every product must
have enough stock. Equivalent retries return the current work-order view without
issuing twice.

## Work-order completion

Completion requires every line to satisfy:

```text
QuantityIssued >= QuantityRequested
```

The current model has no short-close reason, approved quantity reduction,
substitution, reservation, or back-order workflow. Those require an RFC and new
acceptance criteria.

## HTTP errors

- `400`: malformed JSON, validation, or domain-rule failure.
- `401`: missing/invalid authentication or refresh token.
- `403`: authenticated role lacks permission.
- `404`: referenced resource not found.
- `409`: optimistic-concurrency or idempotency-key conflict.
- `413`: request exceeds the one-megabyte server body limit.
- `429`: authentication rate limit exceeded.
- `500`: generic unexpected failure; diagnostic details remain in logs.

## Activity timeline

`/api/v1/audit` combines entity metadata into a derived activity view. It is not
a complete, immutable, or tamper-evident audit journal. Stock movements are the
only records explicitly protected as append-only in the current design.
Authorized historical queries intentionally bypass soft-delete filters so
deleted unreferenced products/users and movement attribution remain readable.
