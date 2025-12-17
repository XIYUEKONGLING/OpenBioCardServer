# OBCS (OpenBioCardServer) API 文档

**数据格式**: `application/json`

## 目录

1. [通用说明](#1-通用说明)
2. [系统接口](#2-系统接口)
3. [认证接口](#3-认证接口)
4. [用户接口](#4-用户接口)
5. [管理员接口](#5-管理员接口)
6. [数据模型参考](#6-数据模型参考)

---

## 1. 通用说明

### 认证方式
部分接口需要身份验证。
- **Header 方式**: 在 HTTP 请求头中添加 `Authorization: Bearer <your_token>`。
- **Body 方式**: 部分接口（如删除账号、管理员操作）需要在请求体 JSON 中包含 `token` 字段。

### 错误处理
- **200 OK**: 请求成功。
- **400 Bad Request**: 请求参数错误。
- **401 Unauthorized**: Token 无效或缺失。
- **403 Forbidden**: 权限不足（例如非管理员尝试执行管理操作）。
- **404 Not Found**: 资源不存在。
- **500 Internal Server Error**: 服务器内部错误。

---

## 2. 系统接口

### 检查后端状态
获取后端版本及环境信息。

- **接口**: `GET /check-backend`
- **权限**: 公开

**响应示例**:
```json
{
  "backend": "OpenBioCardServer",
  "version": "1.0.0",
  "environment": "Development",
  "buildDate": null
}
```

---

## 3. 认证接口

### 用户注册
注册一个新的普通用户或管理员账号。

- **接口**: `POST /signup/create`
- **权限**: 公开

**请求参数**:

| 字段 | 类型 | 必填 | 说明 |
|---|---|---|---|
| username | string | 是 | 用户名 (唯一) |
| password | string | 是 | 密码 |
| type | string | 否 | 用户类型: `user` (默认) 或 `admin`。注意：不能注册 `root`。 |

**请求示例**:
```json
{
  "username": "newuser",
  "password": "secretpassword",
  "type": "user"
}
```

**响应示例**:
```json
{
  "token": "guid-token-string..."
}
```

### 用户登录
- **接口**: `POST /signin`
- **权限**: 公开

**请求参数**:

| 字段 | 类型 | 必填 | 说明 |
|---|---|---|---|
| username | string | 是 | 用户名 |
| password | string | 是 | 密码 |

**响应示例**:
```json
{
  "token": "guid-token-string..."
}
```

### 删除账号 (注销)
用户删除自己的账号。

- **接口**: `POST /delete`
- **权限**: 需提供 Token

**请求参数**:

| 字段 | 类型 | 必填 | 说明 |
|---|---|---|---|
| username | string | 是 | 要删除的用户名 |
| token | string | 是 | 当前用户的 Token |

**响应示例**:
```json
{
  "message": "Account deleted successfully"
}
```

---

## 4. 用户接口

### 获取用户资料
获取指定用户的公开资料信息。

- **接口**: `GET /user/{username}`
- **权限**: 公开

**响应示例**:
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
  // ... 其他 User 字段
}
```

### 更新用户资料
更新当前用户的个人资料。

- **接口**: `POST /user/{username}`
- **权限**: 需要认证 (Header)
- **Header**: `Authorization: Bearer <token>`

**请求参数**:
请求体为完整的 `User` 对象结构（参考下方[数据模型](#6-数据模型参考)）。

**请求示例**:
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

**响应示例**:
```json
{
  "success": true
}
```

---

## 5. 管理员接口

**注意**: 所有管理员接口均要求发起请求的用户 `type` 为 `admin` 或 `root`。

### 检查管理员权限
验证当前 Token 是否拥有管理员权限。

- **接口**: `POST /admin/check-permission`

**请求参数**:
```json
{
  "username": "admin_user",
  "token": "admin_token..."
}
```

**响应示例**:
```json
{
  "success": true,
  "type": "admin"
}
```

### 获取用户列表 (POST 方式)
- **接口**: `POST /admin/users/list`

**请求参数**:
```json
{
  "token": "admin_token..."
}
```

**响应示例**:
```json
{
  "users": [
    { "username": "user1", "type": "user" },
    { "username": "admin1", "type": "admin" }
  ]
}
```

### 获取用户列表 (GET 方式)
- **接口**: `GET /admin/users`
- **认证方式 (二选一)**:
    1. Header: `Authorization: Bearer <token>`
    2. Query Param: `?token=<token>`

**响应示例**: 同上。

### 创建用户 (管理员操作)
管理员直接创建新用户，无需注册流程。

- **接口**: `POST /admin/users`

**请求参数**:
| 字段 | 类型 | 必填 | 说明 |
|---|---|---|---|
| token | string | 是 | 管理员 Token |
| newUsername | string | 是 | 新用户的用户名 |
| password | string | 是 | 新用户的密码 |
| type | string | 否 | `user` 或 `admin` |

**响应示例**:
```json
{
  "message": "User created",
  "token": "new-user-token..."
}
```

### 删除用户 (管理员操作)
管理员删除指定用户。

- **接口**: `DELETE /admin/users/{username}`
- **注意**: 此接口虽为 DELETE 方法，但需要 Request Body 传递 Token。

**请求参数 (Body)**:
```json
{
  "token": "admin_token..."
}
```

**响应示例**:
```json
{
  "message": "User deleted"
}
```

---

## 6. 数据模型参考

### User (用户资料对象)
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