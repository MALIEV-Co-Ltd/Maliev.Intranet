namespace Maliev.Intranet.Shared;

/// <summary>
/// Central authority for all permissions available in the Maliev platform.
/// </summary>
public static class MalievPermissions
{
    /// <summary>
    /// Permissions for Identity and Access Management operations.
    /// </summary>
    public static class IAM
    {
        /// <summary>Permission to manage IAM policies, users, and roles.</summary>
        public const string Manage = "iam.principals.manage";
        /// <summary>Permission to read IAM information.</summary>
        public const string Read = "iam.principals.read";
    }

    /// <summary>
    /// Permissions for Identity and Access Management operations (casing-insensitive alias).
    /// </summary>
    public static class Iam
    {
        /// <summary>Permission to manage IAM policies, users, and roles.</summary>
        public const string Manage = "iam.principals.manage";
        /// <summary>Permission to read IAM information.</summary>
        public const string Read = "iam.principals.read";
    }

    /// <summary>
    /// Permissions for BFF authentication session endpoints.
    /// </summary>
    public static class Auth
    {
        /// <summary>Permission to read the current authenticated session.</summary>
        public const string SessionsRead = "auth.sessions.read";
    }

    /// <summary>
    /// Permissions for chat sessions and messages.
    /// </summary>
    public static class Chat
    {
        /// <summary>Permission to create chat sessions and messages.</summary>
        public const string SessionsCreate = "chat.sessions.create";
    }

    /// <summary>
    /// Permissions for customer management and profile operations.
    /// </summary>
    public static class Customer
    {
        /// <summary>Permission to read individual customer records.</summary>
        public const string Read = "customer.customers.read";
        /// <summary>Permission to list and search customers.</summary>
        public const string List = "customer.customers.list";
        /// <summary>Permission to create or update customer records.</summary>
        public const string Write = "customer.customers.write";

        /// <summary>
        /// Permissions for customer profile specific operations.
        /// </summary>
        public static class Profile
        {
            /// <summary>Permission to read customer profile details.</summary>
            public const string Read = "customer.profile.read";
            /// <summary>Permission to update customer profile information.</summary>
            public const string Write = "customer.profile.write";
        }
    }

    /// <summary>
    /// Permissions for order lifecycle management.
    /// </summary>
    public static class Order
    {
        /// <summary>Permission to view order details and history.</summary>
        public const string Read = "order.orders.read";
        /// <summary>Permission to create, update, or cancel orders.</summary>
        public const string Write = "order.orders.write";
    }

    /// <summary>
    /// Permissions for accessing the central registry of locations and companies.
    /// </summary>
    public static class Registry
    {
        /// <summary>Permission to view registered facility and site locations.</summary>
        public const string LocationsRead = "registry.locations.read";
        /// <summary>Permission to view registered external companies and partners.</summary>
        public const string CompaniesRead = "registry.companies.read";
    }

    /// <summary>
    /// Permissions for managing external supplier relationships.
    /// </summary>
    public static class Supplier
    {
        /// <summary>Permission to view supplier profiles and ratings.</summary>
        public const string Read = "supplier.suppliers.read";
        /// <summary>Permission to onboard or update suppliers.</summary>
        public const string Write = "supplier.suppliers.write";
        /// <summary>Permission to remove supplier records from the system.</summary>
        public const string Delete = "supplier.suppliers.delete";
    }

    /// <summary>
    /// Permissions for quotation and proposal management.
    /// </summary>
    public static class Quotation
    {
        /// <summary>Permission to view quotations and pricing proposals.</summary>
        public const string Read = "quotation.quotations.read";
        /// <summary>Permission to create or edit quotations.</summary>
        public const string Write = "quotation.quotations.write";
        /// <summary>Permission to formally approve quotations for conversion to orders.</summary>
        public const string Approve = "quotation.quotations.approve";
    }

    /// <summary>
    /// Permissions for raw material and inventory catalog management.
    /// </summary>
    public static class Material
    {
        /// <summary>Permission to view material specifications and stock levels.</summary>
        public const string Read = "material.materials.read";
        /// <summary>Permission to define new materials or update inventory details.</summary>
        public const string Write = "material.materials.write";
    }

    /// <summary>
    /// Permissions for financial invoice processing.
    /// </summary>
    public static class Invoice
    {
        /// <summary>Permission to view sales and purchase invoices.</summary>
        public const string Read = "invoice.invoices.read";
        /// <summary>Permission to update existing invoice records.</summary>
        public const string Write = "invoice.invoices.write";
        /// <summary>Permission to generate new invoices (mapped to Write).</summary>
        public const string Create = "invoice.invoices.write"; // Map to Write
    }

    /// <summary>
    /// Permissions for employee directory and HR record management.
    /// </summary>
    public static class Employee
    {
        /// <summary>Permission to view employee profiles and organization structure.</summary>
        public const string Read = "employee.employees.read";
        /// <summary>Permission to manage employee data and assignments.</summary>
        public const string Write = "employee.employees.write";
    }

    /// <summary>
    /// Permissions for leave management and balance tracking.
    /// </summary>
    public static class Leave
    {
        /// <summary>Permission to view personal or team leave balances.</summary>
        public const string Read = "leave.balances.read";
        /// <summary>Permission to submit new leave requests.</summary>
        public const string Write = "leave.requests.create";
    }

    /// <summary>
    /// Permissions for recruitment, job postings, and career reports.
    /// </summary>
    public static class Career
    {
        /// <summary>Permission to view active job vacancies.</summary>
        public const string Read = "career.job-postings.read";
        /// <summary>Permission to manage job postings and applicant data.</summary>
        public const string Write = "career.job-postings.write";
        /// <summary>Permission to view recruitment and career advancement statistics.</summary>
        public const string Stats = "career.reports.read";
    }

    /// <summary>
    /// Permissions for payment processing and transaction history.
    /// </summary>
    public static class Payment
    {
        /// <summary>Permission to view payment transactions and status.</summary>
        public const string Read = "payment.payments.read";
        /// <summary>Permission to initiate or process payments.</summary>
        public const string Write = "payment.payments.write";
    }

    /// <summary>
    /// Permissions for AI-driven data extraction and analysis services.
    /// </summary>
    public static class Prediction
    {
        /// <summary>Permission to trigger automated data extraction from documents.</summary>
        public const string Extract = "prediction.extractions.extract";
    }

    /// <summary>
    /// Permissions for viewing management dashboards.
    /// </summary>
    public static class Dashboard
    {
        /// <summary>Permission to access and view system-wide dashboards.</summary>
        public const string View = "dashboard.view";
    }

    /// <summary>
    /// Permissions for system health monitoring and diagnostic tools.
    /// </summary>
    public static class System
    {
        /// <summary>Permission to view system health status.</summary>
        public const string HealthRead = "system.health.read";
        /// <summary>Permission to access system diagnostic logs and metrics.</summary>
        public const string DiagnosticsRead = "system.diagnostics.read";
    }

    // New Domains

    /// <summary>
    /// Permissions for general accounting and ledger operations.
    /// </summary>
    public static class Accounting
    {
        /// <summary>Permission to view chart of accounts and balances.</summary>
        public const string Read = "accounting.accounts.read";
        /// <summary>Permission to manage accounts and financial configurations.</summary>
        public const string Write = "accounting.accounts.write";

        /// <summary>
        /// Permissions for journal entry management.
        /// </summary>
        public static class Journal
        {
            /// <summary>Permission to view accounting journal entries.</summary>
            public const string Read = "accounting.journals.read";
            /// <summary>Permission to post or adjust journal entries.</summary>
            public const string Write = "accounting.journals.write";
        }
    }

    /// <summary>
    /// Permissions for managing payment receipts and evidence.
    /// </summary>
    public static class Receipt
    {
        /// <summary>Permission to view payment receipts.</summary>
        public const string Read = "receipt.receipts.read";
        /// <summary>Permission to upload or update receipt documentation.</summary>
        public const string Write = "receipt.receipts.write";
    }

    /// <summary>
    /// Permissions for employee onboarding tasks and workflows.
    /// </summary>
    public static class Onboarding
    {
        /// <summary>Permission to view assigned onboarding tasks.</summary>
        public const string Read = "onboarding.tasks.read";
        /// <summary>Permission to manage onboarding checklists and task status.</summary>
        public const string Write = "onboarding.tasks.write";
    }

    /// <summary>
    /// Permissions for procurement via purchase orders.
    /// </summary>
    public static class PurchaseOrder
    {
        /// <summary>Permission to view purchase order details.</summary>
        public const string Read = "purchase-order.orders.read";
        /// <summary>Permission to create and manage purchase orders with suppliers.</summary>
        public const string Write = "purchase-order.orders.write";
    }

    /// <summary>
    /// Permissions for regulatory compliance and audit record management.
    /// </summary>
    public static class Compliance
    {
        /// <summary>Permission to view compliance records and audit trails.</summary>
        public const string Read = "compliance.records.read";
        /// <summary>Permission to log compliance activities and updates.</summary>
        public const string Write = "compliance.records.write";
    }

    /// <summary>
    /// Permissions for employee performance reviews and appraisals.
    /// </summary>
    public static class Performance
    {
        /// <summary>Permission to view performance review outcomes.</summary>
        public const string Read = "performance.reviews.read";
        /// <summary>Permission to conduct appraisals and manage review cycles.</summary>
        public const string Write = "performance.reviews.write";
    }

    /// <summary>
    /// Permissions for managing employee compensation, salaries, and benefits.
    /// </summary>
    public static class Compensation
    {
        /// <summary>Permission to view salary and benefit information.</summary>
        public const string Read = "compensation.salaries.read";
        /// <summary>Permission to update compensation packages and payroll data.</summary>
        public const string Write = "compensation.salaries.write";
    }

    /// <summary>
    /// Permissions for managing billing notes and associated documentation.
    /// </summary>
    public static class BillingNote
    {
        /// <summary>Permission to view billing notes.</summary>
        public const string Read = "billing.notes.read";
        /// <summary>Permission to create or update billing-related annotations.</summary>
        public const string Write = "billing.notes.write";
    }

    /// <summary>
    /// Permissions for managing platform-wide and user-specific preferences.
    /// </summary>
    public static class Preference
    {
        /// <summary>Permission to read system or user settings.</summary>
        public const string Read = "preference.preferences.read";
        /// <summary>Permission to update configuration and preference values.</summary>
        public const string Write = "preference.preferences.write";
    }

    /// <summary>
    /// Permissions for notification template management.
    /// </summary>
    public static class Notification
    {
        /// <summary>Permission to view email and message templates.</summary>
        public const string ReadTemplate = "notification.templates.read";
        /// <summary>Permission to define or update notification templates.</summary>
        public const string WriteTemplate = "notification.templates.write";
    }

    /// <summary>
    /// Permissions for core pricing engine configuration.
    /// </summary>
    public static class Pricing
    {
        /// <summary>Permission to view pricing rules and configurations.</summary>
        public const string ReadConfig = "pricing.configuration.read";
        /// <summary>Permission to manage pricing strategies and parameter values.</summary>
        public const string WriteConfig = "pricing.configuration.write";
    }

    /// <summary>
    /// Permissions for logistics and delivery note management.
    /// </summary>
    public static class Delivery
    {
        /// <summary>Permission to view delivery notes and shipping status.</summary>
        public const string Read = "delivery.delivery-notes.read";
        /// <summary>Permission to create new delivery documentation.</summary>
        public const string Create = "delivery.delivery-notes.create";
        /// <summary>Permission to update existing delivery details.</summary>
        public const string Update = "delivery.delivery-notes.update";
        /// <summary>Permission to delete delivery records.</summary>
        public const string Delete = "delivery.delivery-notes.delete";
        /// <summary>Permission to modify the lifecycle status of a delivery.</summary>
        public const string UpdateStatus = "delivery.delivery-notes.update-status";
        /// <summary>Permission to generate PDF representations of delivery notes.</summary>
        public const string GeneratePdf = "delivery.delivery-notes.generate-pdf";
    }

    /// <summary>
    /// Permissions for managing physical facilities, equipment, and maintenance.
    /// </summary>
    public static class Facility
    {
        /// <summary>Read equipment information, notes, loans, maintenance logs and attachments.</summary>
        public const string Read = "facility.equipments.read";

        /// <summary>Create and update equipment, add notes, manage loans, log maintenance and manage attachments.</summary>
        public const string Write = "facility.equipments.write";

        /// <summary>Delete equipment and manage equipment lifecycle (decommission).</summary>
        public const string Manage = "facility.equipments.manage";

        /// <summary>Approve or reject equipment loan requests.</summary>
        public const string LoansApprove = "facility.loans.approve";
    }

    /// <summary>
    /// Permissions for project management, parts tracking, and pricing.
    /// </summary>
    public static class Project
    {
        /// <summary>View projects, parts, pricing, and quotation status.</summary>
        public const string Read = "project.projects.read";

        /// <summary>Create and update projects, add parts, confirm prices, and generate quotations.</summary>
        public const string Write = "project.projects.write";
    }

    /// <summary>
    /// Permissions for manufacturing job management.
    /// </summary>
    public static class Job
    {
        /// <summary>View jobs in production queue, stats, and QR codes.</summary>
        public const string Read = "job.jobs.read";

        /// <summary>Update job status, assign machines, and modify job details.</summary>
        public const string Write = "job.jobs.write";
    }
}
