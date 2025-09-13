# API Documentation

## Endpoints

### POST /api/orders
Create a new order with idempotency support.

**Headers:**
- `Idempotency-Key`: Required. Unique identifier for this request
- `Content-Type`: application/json

**Request Body:**
```json
{
  "customerId": "string",
  "items": [
    {
      "productId": "string",
      "quantity": number,
      "price": number
    }
  ],
  "totalAmount": number
}
```

**Response:**
```json
{
  "orderId": "string",
  "status": "created|cached",
  "timestamp": "ISO8601",
  "totalAmount": number
}
```

### GET /api/orders/{orderId}
Retrieve order details.

**Response:**
```json
{
  "orderId": "string",
  "customerId": "string",
  "items": [...],
  "status": "pending|completed|failed",
  "createdAt": "ISO8601"
}
```
