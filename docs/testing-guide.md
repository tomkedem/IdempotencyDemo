# Frontend-Backend API Testing Guide

## Overview
This guide provides step-by-step instructions for testing the Angular frontend connection to the .NET backend API.

## Prerequisites
1. **Backend Requirements:**
   - .NET 8.0 SDK installed
   - SQL Server running on localhost:1433
   - Database schema created (use DatabaseSetupController)

2. **Frontend Requirements:**
   - Node.js 18+ and npm
   - Angular CLI 20+

## Backend Setup & Testing

### 1. Start the Backend API
```bash
cd src/backend/PostalIdempotencyDemo.Api
dotnet run
```
The API should start on `https://localhost:5000`

### 2. Test Backend Endpoints Manually
```bash
# Test basic connectivity
curl https://localhost:5000/api/test/connection

# Test delivery endpoint (POST)
curl -X POST https://localhost:5000/api/idempotency-demo/delivery \
  -H "Content-Type: application/json" \
  -H "Idempotency-Key: test-key-123" \
  -d '{
    "barcode": "TEST001",
    "employeeId": "EMP001",
    "locationLat": 32.0853,
    "locationLng": 34.7818,
    "recipientName": "Test Recipient",
    "deliveryStatus": 1,
    "notes": "Test delivery"
  }'
```

## Frontend Setup & Testing

### 1. Start the Angular Development Server
```bash
cd src/frontend/postal-idempotency-app
npm install
ng serve
```
The app should start on `http://localhost:4200`

### 2. Test Frontend-Backend Connection

#### Using the UI:
1. Navigate to `http://localhost:4200`
2. Go to the "Create Shipment" page
3. Click the "בדוק חיבור API" (Test API Connection) button
4. Check for success/error messages

#### Expected Results:
- **Success:** Green snackbar with "API connected successfully"
- **Failure:** Red snackbar with specific error message

## API Endpoints Configuration

### Backend Endpoints (Port 5000):
- `GET /api/test/connection` - Test database connectivity
- `POST /api/idempotency-demo/delivery` - Create delivery
- `POST /api/idempotency-demo/signature` - Create signature
- `GET /api/test/shipments` - Get recent shipments

### Frontend Configuration:
- Environment: `src/environments/environment.ts`
- API URL: `https://localhost:5000/api`

## Data Model Alignment

### Critical Fix Applied:
The `deliveryStatus` field was updated to use **numeric values** instead of strings:
- Frontend: `deliveryStatus: number` (1=delivered, 2=failed, 3=partial)
- Backend: `DeliveryStatus: int` (1=delivered, 2=failed, 3=partial)

## Common Issues & Solutions

### 1. CORS Errors
**Problem:** Browser blocks requests due to CORS policy
**Solution:** Backend is configured to allow `http://localhost:4200`

### 2. Connection Refused
**Problem:** Frontend cannot reach backend
**Solutions:**
- Verify backend is running on port 5000
- Check firewall settings
- Ensure no other service is using port 5000

### 3. Database Connection Issues
**Problem:** Backend cannot connect to SQL Server
**Solutions:**
- Verify SQL Server is running
- Check connection string in `appsettings.json`
- Run database setup via `/api/database-setup/create-tables`

### 4. Data Type Mismatches
**Problem:** API returns validation errors
**Solution:** Ensure frontend sends correct data types (numbers for status, etc.)

## Testing Checklist

- [ ] Backend API starts without errors
- [ ] Database connection test passes
- [ ] Frontend development server starts
- [ ] API connection test button works
- [ ] Create delivery form submits successfully
- [ ] Shipment lookup retrieves data
- [ ] Status updates work correctly
- [ ] Error handling displays appropriate messages

## Troubleshooting Commands

```bash
# Check if backend is running
netstat -an | findstr :5000

# Check if frontend is running  
netstat -an | findstr :4200

# Test backend health
curl https://localhost:5000/api/test/connection

# View backend logs
# Check console output where `dotnet run` is executed

# View frontend logs
# Open browser developer tools (F12) and check Console tab
```

## Success Indicators

1. **Backend Health:** `/api/test/connection` returns 200 with database success message
2. **Frontend Connection:** Test button shows green success message
3. **End-to-End:** Can create delivery and see it in lookup
4. **Error Handling:** Appropriate error messages for invalid data

## Next Steps

After successful connectivity testing:
1. Test idempotency functionality with duplicate requests
2. Test chaos engineering features
3. Performance testing with multiple concurrent requests
4. Integration testing with real data scenarios
