# staffnex API Handoff Guide

## Base URL

- Development API: `http://localhost:5128`
- Swagger: `http://localhost:5128/swagger`

## Seeded Credentials

- Admin
  - Username: `admin`
  - Password: `Admin@123`
- Staff
  - Username: `raj.staff`
  - Password: `Staff@123`
- Staff
  - Username: `pooja.staff`
  - Password: `Staff@123`
- Staff
  - Username: `amit.staff`
  - Password: `Staff@123`

## Authentication Model

- Authentication type: JWT Bearer
- Login returns:
  - access token
  - refresh token
  - role
  - user id
  - staff id
  - full name
  - employee id
- Protected requests must send header:
  - `Authorization: Bearer <access-token>`

## JWT Claims

- `Id`
- `Username`
- `Role`
- `staffId`
- `fullName`
- `employeeId`

In the current API implementation these are exposed through standard and custom claim keys:

- name identifier
- name
- role
- `staffId`
- `fullName`
- `employeeId`

## Standard Response Shapes

### Success Response

```json
{
  "success": true,
  "message": "Request completed successfully.",
  "data": {}
}
```

### Error Response

```json
{
  "statusCode": 400,
  "title": "Validation Failed",
  "message": "One or more validation errors occurred.",
  "traceId": "...",
  "errors": {
    "FieldName": [
      "Validation message"
    ]
  }
}
```

## Pagination and Sorting

List endpoints support these query params where applicable:

- `page`
- `pageSize`
- `sortBy`
- `sortDirection` with values `asc` or `desc`

Paged response shape:

```json
{
  "success": true,
  "message": "List fetched successfully.",
  "data": {
    "items": [],
    "page": 1,
    "pageSize": 10,
    "totalCount": 3,
    "totalPages": 1
  }
}
```

## Status Rules

- Attendance statuses:
  - `Present`
  - `Absent`
  - `Half-Day`
  - `Leave`
- Half-Day rule:
  - less than `4` working hours
- Salary deduction rule:
  - `absent days × per day salary`
  - per day salary = `monthly salary / working days excluding Sundays`

## Auth Endpoints

### Login

- `POST /api/auth/login`

Request:

```json
{
  "username": "admin",
  "password": "Admin@123",
  "role": "Admin"
}
```

Response:

```json
{
  "success": true,
  "message": "Login successful.",
  "data": {
    "token": "<jwt>",
    "role": "Admin",
    "userId": 1,
    "staffId": null,
    "fullName": "admin",
    "employeeId": "",
    "refreshToken": "<refresh-token>",
    "refreshTokenExpiresAt": "2026-04-12T14:21:18.7081726Z"
  }
}
```

### Refresh Token

- `POST /api/auth/refresh`

Request:

```json
{
  "refreshToken": "<refresh-token>"
}
```

### Logout

- `POST /api/auth/logout`

Request:

```json
{
  "refreshToken": "<refresh-token>"
}
```

## Staff Endpoints

### Get Staff List

- `GET /api/staff?page=1&pageSize=10&search=&departmentId=&isActive=true&sortBy=fullName&sortDirection=asc`
- Admin only

### Get Staff By Id

- `GET /api/staff/{id}`
- Admin can access any staff profile
- Staff can access only own profile

### Get Departments

- `GET /api/staff/departments`
- Admin only

### Create Staff

- `POST /api/staff`
- Admin only

Request:

```json
{
  "fullName": "New Staff",
  "phone": "9876543210",
  "email": "new.staff@staffnex.local",
  "designation": "Field Executive",
  "departmentId": 2,
  "monthlySalary": 28000,
  "joinDate": "2026-04-01T00:00:00Z",
  "username": "new.staff",
  "password": "Staff@123"
}
```

## Attendance Endpoints

### Check In

- `POST /api/attendance/checkin`

```json
{
  "staffId": 1,
  "latitude": 26.912434,
  "longitude": 75.787270,
  "address": "Jaipur Office"
}
```

### Check Out

- `POST /api/attendance/checkout`

```json
{
  "staffId": 1,
  "latitude": 26.912500,
  "longitude": 75.787300,
  "address": "Jaipur Office Exit"
}
```

### Get Today By Staff

- `GET /api/attendance/today/{staffId}`

### Get Today All

- `GET /api/attendance/today-all?page=1&pageSize=10&staffId=&status=&sortBy=logDate&sortDirection=desc`
- Admin only

### Get Monthly By Staff

- `GET /api/attendance/monthly/{staffId}?year=2026&month=4&page=1&pageSize=10&status=&sortBy=logDate&sortDirection=desc`

### Get Monthly All

- `GET /api/attendance/all-monthly?year=2026&month=4&page=1&pageSize=10&staffId=&status=&sortBy=logDate&sortDirection=desc`
- Admin only

## Location Endpoints

### Update Location

- `POST /api/location/update`

```json
{
  "staffId": 1,
  "latitude": 26.912600,
  "longitude": 75.787350,
  "address": "Client Visit Route"
}
```

### Get All Active Locations

- `GET /api/location/all-active`
- Admin only

### Get Location Trail

- `GET /api/location/trail/{staffId}?date=2026-04-05`

## Leave Endpoints

### Create Leave Request

- `POST /api/leave-requests`

```json
{
  "staffId": 2,
  "leaveDate": "2026-04-10T00:00:00Z",
  "reason": "Medical appointment"
}
```

### Get Leave Requests

- `GET /api/leave-requests?page=1&pageSize=10&status=Pending&sortBy=leaveDate&sortDirection=desc`
- Admin gets all
- Staff gets own only

### Get Leave Requests By Staff

- `GET /api/leave-requests/staff/{staffId}?page=1&pageSize=10&status=&fromDate=&toDate=&sortBy=leaveDate&sortDirection=desc`

### Update Leave Request

- `PUT /api/leave-requests/{id}`

### Approve Leave Request

- `PATCH /api/leave-requests/{id}/approve`

```json
{
  "remarks": "Approved for medical leave"
}
```

### Reject Leave Request

- `PATCH /api/leave-requests/{id}/reject`

```json
{
  "remarks": "Insufficient leave balance"
}
```

### Delete Leave Request

- `DELETE /api/leave-requests/{id}`

## Report Endpoints

### Dashboard

- `GET /api/report/dashboard`
- Admin only

### All Staff Performance

- `GET /api/report/performance?year=2026&month=4`
- Admin only

### Single Staff Performance

- `GET /api/report/performance/{staffId}?year=2026&month=4`

## Frontend Integration Notes

- Store access token in memory or secure client storage based on app type.
- Store refresh token carefully and rotate it after each refresh.
- On `401`, try refresh once, then force re-login.
- On `403`, show permission error instead of redirect loops.
- All GPS fields are decimal-compatible values.
- For staff users, use `staffId` from login response instead of hardcoding ids.

## Recommended Frontend Flow

1. Login with username, password, role.
2. Save `token`, `refreshToken`, `role`, `staffId`, `fullName`, `employeeId`.
3. Use bearer token for all protected API calls.
4. On token expiry, call `/api/auth/refresh`.
5. Replace both access and refresh tokens with the new values.
6. On logout, call `/api/auth/logout` with the current refresh token.

## Postman Collection

- Available at: `Postman/staffnex.postman_collection.json`
