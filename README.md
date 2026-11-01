# Development of Diagnostic Management System for LabSync

## Project Overview

**LabSync** is a web-based Diagnostic ERP Management System developed to streamline and automate the daily operations of diagnostic centers and laboratories.

The system provides an integrated platform for managing patients, employees, appointments, diagnostic tests, sample collection, laboratory test results, report generation, billing, and payment processing.

It features role-based access control for Admin, Receptionist, and Lab Technician, ensuring that each user can efficiently perform their assigned responsibilities.

The system aims to improve operational efficiency, reduce manual work, ensure accurate record management, and provide a seamless diagnostic service management experience.

## User Roles

The system supports three primary user roles:

- **Admin:** Manages the overall system, including employees, branches, departments, diagnostic tests, reports, invoices, and system activities.
- **Receptionist:** Handles patient registration, appointment scheduling, test assignments, sample collection tracking, billing, and payment management.
- **Lab Technician:** Manages assigned diagnostic tests, sample processing, test result entry, and laboratory report-related activities.

## Key Features

### 1. Authentication & User Management
- Secure User Authentication and Logout
- Role-Based Access Control
- Patient Management
- Employee Management
- Branch Management
- Department Management
- Referral Doctor Management

### 2. Diagnostic Test Management
- Test Category Management
- Test Management
- Sample Type Management
- Appointment Scheduling
- Multiple Test Assignment Under a Single Appointment

### 3. Laboratory Operations
- Sample Collection Tracking
- Sample Status Management (Pending, Collected, Processing, Completed)
- Test Result Management
- Report Approval
- Complete Patient Journey Tracking

### 4. Billing & Payment Management
- Automated PDF Invoice Generation
- Invoice Management
- Payment Management
- Multiple Payment Methods
- Payment History Tracking
- Paid, Unpaid, and Partially Paid Invoice Tracking

### 5. Reports & Analytics
- Automated PDF Laboratory Report Generation
- Report and Invoice Printing
- Dashboard & Analytics
- Daily Revenue and Patient Statistics
- Pending Reports and Completed Test Statistics
- Audit & Activity Tracking

### 6. Additional Features
- Advanced Search and Filtering
- Responsive User Interface
- Multi-Branch Diagnostic Center Management

## Project Screenshots

### 1. Login Page

![Login Page](wwwroot/Screenshots/Login.png)

### 2. Admin Dashboard

![Admin Dashboard](wwwroot/Screenshots/admin-dashboard.png)

### 3. Receptionist Dashboard

![Receptionist Dashboard](wwwroot/Screenshots/receptionist-dashboard.png)

### 4. Lab Technician Dashboard

![Lab Technician Dashboard](wwwroot/Screenshots/lab-technician-dashboard.png)

## Patient Management Workflow

The system integrates the complete diagnostic service workflow:

**Patient Registration → Appointment Scheduling → Test Assignment → Sample Collection → Sample Processing → Test Result Entry → Report Approval → Report Generation → Billing → Payment**

This integrated workflow helps manage the entire patient journey within a centralized system.

## Technology Stack

- **Backend:** ASP.NET Core, C#
- **Authentication:** ASP.NET Core Identity
- **Frontend:** HTML, CSS, JavaScript
- **Database:** [Add your database technology]
- **ORM:** [Add if applicable]
- **UI Framework:** [Add if applicable]

## Installation & Setup

1. Clone the repository:

   ```bash
   git clone YOUR_GITHUB_REPOSITORY_URL
   ```

2. Open the solution file (`.sln`) in Visual Studio.

3. Configure the database connection string in your application configuration.

4. Restore the required NuGet packages.

5. Apply database migrations if required.

6. Build and run the application.

## Project Information

**Project Title:** Development of Diagnostic Management System for LabSync

**Project Type:** Web-Based ERP Management System

**Domain:** Healthcare & Diagnostic Center Management

**Development Environment:** Visual Studio

---

*LabSync – An Integrated Diagnostic ERP Management Solution.*
