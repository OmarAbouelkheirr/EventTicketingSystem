# 🎫 EventHub

> A comprehensive web platform that helps students and individuals discover and book events in fields they’re passionate about.

We noticed that many students struggle to find relevant events in their areas of interest. We built **EventHub** to bridge this gap by making it easy to browse, book, and attend events — all in one centralized place.

---

## ✨ Features

- **Role-Based Access Control:** Secure login system with distinct privileges for Admins, Organizers, and Attendees (using JWT Authentication).
- **Attendee Portal:** Users can browse upcoming events, book tickets, and receive a unique QR code for their ticket.
- **Organizer Dashboard:** Users can register as event organizers to create, manage, and monitor their own events.
- **On-Site Verification:** Organizers can scan attendee QR codes directly at the event gate to verify tickets on the spot.
- **Admin Dashboard:** A comprehensive control panel for administrators to manage users, events, and platform content.

## 🏗️ Architecture & Modules

The system is modularized into several key components:

### 1. Backend / API (`/Project`)
- Powered by **.NET Core (MVC & Web API)**
- **Database Access:** Entity Framework & LINQ
- **Security:** JWT Authentication
- Implements the core business logic, API endpoints, and database interactions.

### 2. Frontends (`/Frontend`)
- **Frontend User:** The main public-facing web interface where users can discover and book events.
- **Frontend Org Dashboard:** A dedicated portal for event organizers to manage their events and scan tickets.
- **Frontend Admin Dashboard:** A management interface for system administrators.
- **UI Tech Stack:** HTML, CSS, JavaScript, Bootstrap.

## 🚀 Tech Stack

**Client-Side:**
- HTML5, CSS3, JavaScript
- Bootstrap (Responsive Design)

**Server-Side:**
- C# .NET Core (MVC & Web API)
- Entity Framework Core
- LINQ
- JWT (JSON Web Tokens) for Authentication

## 👥 Team

*Add your team members here:*

- **[Team Member 1 Name]** - *[Role/Title]* - [Link to GitHub/LinkedIn]
- **[Team Member 2 Name]** - *[Role/Title]* - [Link to GitHub/LinkedIn]
- **[Team Member 3 Name]** - *[Role/Title]* - [Link to GitHub/LinkedIn]
- **[Team Member 4 Name]** - *[Role/Title]* - [Link to GitHub/LinkedIn]
- **[Team Member 5 Name]** - *[Role/Title]* - [Link to GitHub/LinkedIn]

## 🛠️ Setup & Installation

*(Instructions on how to run the project locally will go here)*

1. Clone the repository.
2. Open the Backend `.sln` file in Visual Studio.
3. Update `appsettings.json` with your database connection string.
4. Run Entity Framework migrations to set up the database.
5. Launch the backend API.
6. Serve the Frontend applications using Live Server or any static file server.

---
*Built with ❤️ by the EventHub Team*