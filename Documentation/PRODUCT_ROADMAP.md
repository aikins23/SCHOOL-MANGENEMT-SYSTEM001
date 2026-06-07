# Product Roadmap & Future Enhancements
# Kingdom Preparatory School Management System

**Date:** June 6, 2026
**Document Purpose:** To outline the strategic roadmap for expanding the application from a core administrative tool into a comprehensive, enterprise-grade educational platform.

This roadmap is divided into thematic phases, prioritized by the value they add to school administrators, parents, and students.

---

## Phase 1: Communication & Parent Experience (High Priority)
*These features bridge the gap between the school and parents, providing immediate, visible value to the school's clients.*

### 1.1 SMS & Email Gateway Integration
*   **Description:** Integrate third-party communication APIs (e.g., Twilio, Africa's Talking, or Hubtel).
*   **Use Cases:** 
    *   Automated fee reminders when a student's balance exceeds a certain threshold.
    *   Immediate absence alerts to parents.
    *   Bulk SMS for PTA meetings, exam schedules, and holiday announcements.

### 1.2 Parent Web Portal (Read-Only)
*   **Description:** A lightweight ASP.NET Core Web Application linked to the central database.
*   **Use Cases:** 
    *   Parents can log in securely using their ward's Student ID and a PIN.
    *   View outstanding fee balances, download official report cards (PDF), and view term attendance without visiting the school administration office.

### 1.3 Online Fee Payments (Mobile Money Integration)
*   **Description:** Incorporate a payment gateway (e.g., Paystack, Flutterwave) into the Parent Web Portal.
*   **Use Cases:**
    *   Allow parents to pay school fees directly via Mobile Money or debit card.
    *   Automatically sync successful online payments into the desktop application's `frmFessPayment` module and generate an electronic receipt.

---

## Phase 2: Academic & Student Life Automation (Medium Priority)
*These features reduce the manual workload for teachers and administrative staff.*

### 2.1 Automated Attendance Tracking (Biometric/Barcode)
*   **Description:** Support USB barcode scanners or basic biometric (fingerprint) readers.
*   **Use Cases:**
    *   Students swipe their ID cards upon entry. The system automatically marks them "Present".
    *   Integrates with the SMS Gateway to text parents if a student has not arrived by a designated cutoff time.

### 2.2 Library Management Module
*   **Description:** A sub-system to manage school reading materials.
*   **Use Cases:**
    *   Catalog books by ISBN, Author, and Genre.
    *   Track borrowing and enforce return dates.
    *   Automatically calculate overdue fines and append them to the student's central financial account.

### 2.3 Automated Timetable Generation
*   **Description:** An algorithmic scheduling tool.
*   **Use Cases:**
    *   Input teachers, subjects, classroom availability, and constraints (e.g., "Math must be before lunch").
    *   The system generates clash-free weekly timetables, eliminating hours of manual administrative planning.

---

## Phase 3: Logistics & Administrative Efficiency (Strategic Value)
*These features help the school track physical assets and manage secondary revenue streams.*

### 3.1 Transport & Fleet Management
*   **Description:** A module to manage school buses and transport routes.
*   **Use Cases:**
    *   Assign students to specific bus routes and automatically bill them for "Transport Fees".
    *   Track fleet maintenance, insurance renewal dates, and fuel expenditure.

### 3.2 Inventory & Asset Tracking
*   **Description:** Track physical school property (computers, laboratory equipment, desks).
*   **Use Cases:**
    *   Record purchase dates, item conditions, and physical locations.
    *   Assign specific assets to staff members to ensure accountability and prevent loss.

---

## Phase 4: Technical Evolution & Scaling
*Architectural upgrades required as the school grows in student population and staff size.*

### 4.1 Cloud Database Migration
*   **Description:** Transition the data store from a local file-based or LocalDB instance to a cloud-hosted SQL Server (e.g., Microsoft Azure SQL Database).
*   **Benefit:** Enables multi-user concurrency across different physical devices. The Principal, Bursar, and IT Admin can log in from completely different computers (or even from home) and interact with real-time data simultaneously.

### 4.2 Role-Based Custom Dashboards
*   **Description:** Evolve the current unified dashboard (`frmDashboard.cs`) into specialized views.
*   **Benefit:** 
    *   **Bursar View:** Focuses heavily on revenue charts, outstanding balances, and recent transactions.
    *   **Teacher View:** Focuses on class lists, grading inputs, and attendance marking.
    *   **Admin View:** Focuses on system health, user management, and school-wide analytics.
