# 📘 User Management API – Minimal ASP.NET Core

This project is a lightweight RESTful API built with ASP.NET Core Minimal API. It provides basic user management functionality with secure JWT-based authentication, input validation, structured error handling, and request/response logging.

---

## 🚀 Features

- **CRUD Operations** for users:
  - `GET /users` – Retrieve all users
  - `GET /users/{id}` – Retrieve user by ID
  - `POST /users` – Add a new user
  - `PUT /users/{id}` – Update user details
  - `DELETE /users/{id}` – Remove user by ID
- **JWT Authentication** – Secure all endpoints
- **FluentValidation** – Validate user input
- **Global Error Handling** – Standardized error responses
- **Request/Response Logging** – Audit all traffic

---

## 🛠️ Setup Instructions

### 1. Clone the Repository

```bash
git clone https://github.com/your-username/UserManagementAPI.git
cd UserManagementAPI
```

### 2. Install Dependencies

```bash
dotnet add package FluentValidation
dotnet add package FluentValidation.DependencyInjectionExtensions
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
```

### 3. Run Api

```bash
dotnet run
```

## 🔐 Authentication

To access protected endpoints, first request a token:

```http
POST /token
```

Then use the returned token in the Authorization header:

```http
Authorization: Bearer <your-token>
```

## 📄 HTTP Test File

Use the included `manage-user.http` file with the **VS Code REST Client** extension to test API endpoints. It includes:

- 🔐 **Token generation** – request a JWT token from `/token`
- ✅ **Authenticated CRUD requests** – test `GET`, `POST`, `PUT`, and `DELETE` on `/users`
- 🛑 **Validation and error scenarios** – verify input validation and standardized error responses

## 📦 Project Structure

```plaintext
UserManagementAPI/
├── Program.cs
├── manage-user.http
├── README.md
```

## ✅ Validation Rules

- **Name**: Required, minimum 2 characters
- **Email**: Required, must be a valid email format

## 📋 Example User Payload

```json
{
  "name": "Alice",
  "email": "alice@example.com"
}
```
