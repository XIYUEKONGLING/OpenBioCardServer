# OBCS (OpenBioCardServer) API Documentation

**Data Format**: `application/json`

## Table of Contents

1. [General Information](#1-general-information)
2. [System Endpoints](#2-system-endpoints)
3. [Authentication Endpoints](#3-authentication-endpoints)
4. [User Endpoints](#4-user-endpoints)
5. [Admin Endpoints](#5-admin-endpoints)
6. [Data Model Reference](#6-data-model-reference)

---

## 1. General Information

### Authentication Methods
Some endpoints require authentication.
- **Header Method**: Add `Authorization: Bearer <your_token>` to the HTTP request header.
- **Body Method**: Some endpoints (such as deleting accounts or admin operations) require a `token` field inside the request body JSON.

### Error Handling
- **200 OK**: Request successful.
- **400 Bad Request**: Invalid request parameters.
- **401 Unauthorized**: Token is invalid or missing.
- **403 Forbidden**: Insufficient permissions (e.g., a non-admin user attempting an admin operation).
- **404 Not Found**: Resource does not exist.
- **500 Internal Server Error**: Server internal error.

---

## 2. System Endpoints

### Check Backend Status
Get backend version and environment information.

- **Endpoint**: `GET /check-backend`
- **Access**: Public

**Response Example**:
```json
{
  "backend": "OpenBioCardServer",
  "version": "1.0.0",
  "environment": "Development",
  "buildDate": null
}
```

---

## 3. Authentication Endpoints

### User Registration
Register a new standard user or administrator account.

- **Endpoint**: `POST /signup/create`
- **Access**: Public

**Request Parameters**:

| Field | Type | Required | Description |
|---|---|---|---|
| username | string | Yes | Username (Unique) |
| password | string | Yes | Password |
| type | string | No | User type: `user` (default) or `admin`. Note: Cannot register as `root`. |

**Request Example**:
```json
{
  "username": "newuser",
  "password": "secretpassword",
  "type": "user"
}
```

**Response Example**:
```json
{
  "token": "guid-token-string..."
}
```

### User Login
- **Endpoint**: `POST /signin`
- **Access**: Public

**Request Parameters**:

| Field | Type | Required | Description |
|---|---|---|---|
| username | string | Yes | Username |
| password | string | Yes | Password |

**Response Example**:
```json
{
  "token": "guid-token-string..."
}
```

### Delete Account (Unsubscribe)
User deletes their own account.

- **Endpoint**: `POST /delete`
- **Access**: Token Required

**Request Parameters**:

| Field | Type | Required | Description |
|---|---|---|---|
| username | string | Yes | The username to delete |
| token | string | Yes | The current user's Token |

**Response Example**:
```json
{
  "message": "Account deleted successfully"
}
```

---

## 4. User Endpoints

### Get User Profile
Get public profile information for a specific user.

- **Endpoint**: `GET /user/{username}`
- **Access**: Public

**Response Example**:
```json
{
  "username": "johndoe",
  "type": "user",
  "name": "John Doe",
  "pronouns": "he/him",
  "bio": "Software Developer",
  "avatar": "url_to_image",
  "contacts": [],
  "socialLinks": [],
  "projects": []
  // ... Other User fields
}
```

### Update User Profile
Update the current user's profile.

- **Endpoint**: `POST /user/{username}`
- **Access**: Authentication Required (Header)
- **Header**: `Authorization: Bearer <token>`

**Request Parameters**:
The request body is the complete `User` object structure (refer to [Data Model Reference](#6-data-model-reference) below).

**Request Example**:
```json
{
  "username": "johndoe",
  "name": "John Doe Updated",
  "bio": "New Bio Content",
  "location": "New York",
  "contacts": [
    { "type": "email", "value": "john@example.com" }
  ]
}
```

**Response Example**:
```json
{
  "success": true
}
```

---

## 5. Admin Endpoints

**Note**: All admin endpoints require the requesting user's `type` to be `admin` or `root`.

### Check Admin Permission
Verify if the current Token has administrator privileges.

- **Endpoint**: `POST /admin/check-permission`

**Request Parameters**:
```json
{
  "username": "admin_user",
  "token": "admin_token..."
}
```

**Response Example**:
```json
{
  "success": true,
  "type": "admin"
}
```

### Get User List (POST Method)
- **Endpoint**: `POST /admin/users/list`

**Request Parameters**:
```json
{
  "token": "admin_token..."
}
```

**Response Example**:
```json
{
  "users": [
    { "username": "user1", "type": "user" },
    { "username": "admin1", "type": "admin" }
  ]
}
```

### Get User List (GET Method)
- **Endpoint**: `GET /admin/users`
- **Authentication Method (Choose one)**:
    1. Header: `Authorization: Bearer <token>`
    2. Query Param: `?token=<token>`

**Response Example**: Same as above.

### Create User (Admin Operation)
Admin creates a new user directly, bypassing the registration flow.

- **Endpoint**: `POST /admin/users`

**Request Parameters**:
| Field | Type | Required | Description |
|---|---|---|---|
| token | string | Yes | Admin Token |
| newUsername | string | Yes | Username for the new user |
| password | string | Yes | Password for the new user |
| type | string | No | `user` or `admin` |

**Response Example**:
```json
{
  "message": "User created",
  "token": "new-user-token..."
}
```

### Delete User (Admin Operation)
Admin deletes a specific user.

- **Endpoint**: `DELETE /admin/users/{username}`
- **Note**: Although this is a DELETE method, it requires a Request Body to transmit the Token.

**Request Parameters (Body)**:
```json
{
  "token": "admin_token..."
}
```

**Response Example**:
```json
{
  "message": "User deleted"
}
```

---

## 6. Data Model Reference

### User (User Profile Object)
```json
{
  "id": "guid...",
  "username": "string",
  "type": "user",
  "name": "string",
  "pronouns": "string?",
  "avatar": "string? (url)",
  "bio": "string?",
  "location": "string?",
  "website": "string?",
  "background": "string? (css/url)",
  "currentCompany": "string?",
  "currentCompanyLink": "string?",
  "currentSchool": "string?",
  "currentSchoolLink": "string?",
  "contacts": [ ContactItem ],
  "socialLinks": [ SocialLinkItem ],
  "projects": [ ProjectItem ],
  "gallery": [ GalleryItem ],
  "createdAt": "datetime"
}
```

### ContactItem
```json
{
  "type": "string (e.g., email, phone)",
  "value": "string"
}
```

### SocialLinkItem
```json
{
  "type": "string (e.g., github, twitter)",
  "value": "string (url)",
  "githubData": {
    "login": "string",
    "name": "string",
    "avatarUrl": "string",
    "bio": "string",
    "followers": 0,
    "publicRepos": 0
  }
}
```

### ProjectItem
```json
{
  "name": "string",
  "url": "string",
  "description": "string",
  "logo": "string?"
}
```

### GalleryItem
```json
{
  "image": "string (url)",
  "caption": "string"
}
```