# Feature Specification: Create Intranet Portal

**Feature Branch**: `001-create-intranet-portal`  
**Created**: 2025-01-07  
**Status**: Draft  
**Input**: User description: "Define the specification for a service named "Maliev.Intranet".Maliev.Intranet is an internal web application used daily by MALIEV employees to manage all operational aspects of the business. This includes customers, orders, quotations, accounting records, suppliers, invoices, receipts, notifications, and task tracking. The system acts as a unified internal interface over multiple backend microservices and does not own core business data beyond UI state and operational metadata.The application must begin with a secure login screen. Authentication is delegated to the IAM service using JWT-based authentication. After successful login, users are redirected to a role-aware dashboard that summarizes the current state of the business, including key metrics, pending tasks, recent activities, alerts, and operational health indicators from connected microservices.The dashboard must aggregate data from multiple services such as Customer, Order, Quotation, Accounting, Notification, and Supplier services via APIs or message-based projections. It must support different views depending on employee roles (e.g. admin, accounting, operations, sales, production).The application must provide structured management screens for:- Customers and contacts- Orders and order status tracking- Quotations and approvals- Invoices, receipts, and accounting documents- Suppliers and procurement records- Internal tasks and follow-ups- System notifications and message historyMaliev.Intranet must not contain business logic that duplicates domain services. All mutations must be executed via the appropriate backend microservice. The intranet acts as an orchestrated UI and workflow layer only.The UI must be optimized for desktop usage and productivity, with fast navigation, dense information layouts, filtering, searching, and keyboard-friendly interactions. Mobile optimization is optional but not required initially.Auditability is required. User actions such as approvals, edits, and submissions must be traceable to an authenticated employee identity.The system must be designed for long-term extensibility, as new microservices and business domains will be added over time without rewriting the core application."

## Clarifications

### Session 2026-01-06
- Q: Frontend Framework & Architecture → A: Blazor
- Q: Dashboard Data Aggregation Pattern → A: Backend-for-Frontend (BFF)
- Q: Blazor Hosting Model → A: Interactive Auto (Server + WASM)
- Q: Dashboard Real-time Capabilities → A: Real-time Push (SignalR)
- Q: Client-Side Observability & Error Tracking → A: OpenTelemetry (OTLP)
- Q: External Identity Provider → A: Google Workspace (Restricted to corporate domain)

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Secure Access via IAM (Priority: P1)

As an employee, I want to log in using my corporate credentials OR my corporate Google Workspace account so that I can securely access the intranet using my preferred method.

**Why this priority**: Access is the prerequisite for all other functionality. Security and auditability depend on verified identity.

**Independent Test**: Can be tested by attempting to log in with:
1. Valid username/password.
2. Valid Google account (matching allowed domain).
3. Invalid Google account (wrong domain).

**Acceptance Scenarios**:

1. **Given** an unauthenticated user, **When** they access the application URL, **Then** they are redirected to the login screen.
2. **Given** a user on the login screen, **When** they click "Login with Google" and authenticate with a valid corporate email, **Then** they are redirected to the dashboard.
3. **Given** a user on the login screen, **When** they authenticate with a personal Gmail account (not in allowlist), **Then** access is denied with a "Domain not authorized" error.
4. **Given** a user on the login screen, **When** they enter valid credentials, **Then** they are authenticated via the IAM service and redirected to the dashboard.

---

### User Story 2 - Role-Aware Operational Dashboard (Priority: P1)

As an authenticated user, I want to see a dashboard customized to my role (e.g., Sales, Accounting) so that I can immediately view relevant metrics, pending tasks, and alerts without searching.

**Why this priority**: This is the landing page that provides immediate value and situational awareness to the employee.

**Independent Test**: Can be tested by logging in with different user roles and verifying that the widgets and data displayed match the role's responsibilities.

**Acceptance Scenarios**:

1. **Given** an authenticated Sales user, **When** they view the dashboard, **Then** they see sales metrics, pending quotations, and recent customer activities.
2. **Given** an authenticated Accounting user, **When** they view the dashboard, **Then** they see pending invoices, payment alerts, and financial summaries.
3. **Given** any authenticated user, **When** they view the dashboard, **Then** they see their personal pending tasks and system notifications.

---

### User Story 3 - Unified Operational Management (Priority: P2)

As an operations employee, I want to access management screens for Customers, Orders, Quotations, and Suppliers from a single interface so that I can perform my daily tasks efficiently without switching tools.

**Why this priority**: This covers the core operational capabilities of the business.

**Independent Test**: Can be tested by navigating to each module (Customers, Orders, etc.) and verifying that data is loaded from the respective backend services and displayed correctly.

**Acceptance Scenarios**:

1. **Given** a user with permissions, **When** they navigate to the "Customers" section, **Then** they see a searchable list of customers fetched from the Customer Service.
2. **Given** a user viewing a specific Order, **When** they view the details, **Then** they see the current status and tracking information aggregated from the Order Service.
3. **Given** a user in the Quotations section, **When** they select a quotation, **Then** they can view its details and approval status.

---

### User Story 4 - Transaction Audit Trail (Priority: P2)

As a compliance officer/admin, I need all user actions (approvals, edits, submissions) to be traceable to the specific employee so that we maintain accountability and audit records.

**Why this priority**: Required for compliance and operational security.

**Independent Test**: Can be tested by performing an action (e.g., updating an order) and verifying that the backend service records the user's ID in the audit log.

**Acceptance Scenarios**:

1. **Given** an authenticated user performing an edit, **When** the change is submitted, **Then** the system passes the user's identity to the backend service.
2. **Given** a historical record, **When** an admin reviews the history, **Then** they can see which user performed the last modification.

---

### Edge Cases

- What happens when a backend microservice is offline? The UI should display a graceful error or "service unavailable" indicator for that specific section without crashing the entire application.
- How does the system handle an expired session? The user should be prompted to re-login without losing their current work if possible, or redirected to login.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST delegate authentication to the Maliev IAM Service and use JWT for session management.
- **FR-002**: System MUST provide a dashboard that aggregates key metrics and alerts from connected microservices (Customer, Order, Quotation, Accounting, Notification, Supplier) using a Backend-for-Frontend (BFF) pattern to optimize data retrieval.
- **FR-003**: System MUST tailor dashboard content and navigation options based on the user's role (Admin, Accounting, Operations, Sales, Production).
- **FR-004**: System MUST provide management interfaces for Customers, Contacts, Orders, Quotations, Invoices, Receipts, Suppliers, and Tasks.
- **FR-005**: System MUST NOT implement core business logic; it MUST execute all data mutations via API calls to the respective backend domain services.
- **FR-006**: System MUST pass authenticated user identity context with every backend request to ensure auditability.
- FR-007**: System MUST be optimized for desktop productivity, supporting keyboard navigation, dense data layouts, and advanced filtering/searching.
- **FR-008**: System MUST support extensibility to add new microservice domains without major architectural rewrites.
- **FR-009**: System MUST be implemented using the Blazor framework with the **Interactive Auto** (Server + WebAssembly) rendering mode to provide both fast initial load and client-side interactivity.
- **FR-010**: System MUST use **SignalR** to provide real-time updates for dashboard metrics and system alerts, ensuring operational awareness without manual refreshes.
- **FR-011**: System MUST implement **OpenTelemetry** for client-side logging and distributed tracing, ensuring full end-to-end observability from the UI to backend microservices.
- **FR-012**: System MUST support **Google OAuth2** authentication, strictly restricted to the corporate email domain (e.g., `@maliev.com`).

### Key Entities *(include if feature involves data)*

- **Dashboard Widget**: Represents a UI component that displays summarized data from a specific microservice (e.g., "Pending Orders", "Recent Invoices").
- **User Session**: Represents the authenticated state of an employee, including their role and permissions.
- **Operational View**: Represents a domain-specific management screen (e.g., "Order Management") that interacts with a specific backend service.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can access their role-specific dashboard within 3 seconds of successful login.
- **SC-002**: 100% of data modification requests to backend services include the authenticated user's identity.
- **SC-003**: Users can navigate between different operational domains (e.g., from Orders to Customers) in 2 clicks or fewer.
- **SC-004**: System supports the integration of at least 6 distinct microservices (Customer, Order, Quotation, Accounting, Notification, Supplier) in the initial release.
- **SC-005**: Management lists (e.g., Orders list) render within 2 seconds for datasets up to 100 items.