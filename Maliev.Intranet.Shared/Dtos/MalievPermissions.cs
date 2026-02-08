namespace Maliev.Intranet.Shared;

/// <summary>
/// Central authority for all permissions available in the Maliev platform.
/// </summary>
public static class MalievPermissions
{
    public static class IAM
    {
        public const string Manage = "iam.manage";
        public const string Read = "iam.read";
    }

    // Alias for consistency with different casings found in code
    public static class Iam
    {
        public const string Manage = "iam.manage";
        public const string Read = "iam.read";
    }

    public static class Customer
    {
        public const string Read = "customer.customers.read";
        public const string Write = "customer.customers.write";
        
        public static class Profile
        {
            public const string Read = "customer.profile.read";
            public const string Write = "customer.profile.write";
        }
    }

    public static class Order
    {
        public const string Read = "order.orders.read";
        public const string Write = "order.orders.write";
    }

    public static class Registry
    {
        public const string LocationsRead = "registry.locations.read";
        public const string CompaniesRead = "registry.companies.read";
    }

    public static class Supplier
    {
        public const string Read = "supplier.suppliers.read";
        public const string Write = "supplier.suppliers.write";
        public const string Delete = "supplier.suppliers.delete";
    }

    public static class Quotation
    {
        public const string Read = "quotation.quotations.read";
        public const string Write = "quotation.quotations.write";
        public const string Approve = "quotation.quotations.approve";
    }

    public static class Material
    {
        public const string Read = "material.materials.read";
        public const string Write = "material.materials.write";
    }

    public static class Invoice
    {
        public const string Read = "invoice.invoices.read";
        public const string Write = "invoice.invoices.write";
    }

    public static class Employee
    {
        public const string Read = "employee.employees.read";
        public const string Write = "employee.employees.write";
    }

    public static class Leave
    {
        public const string Read = "leave.balances.read";
        public const string Write = "leave.requests.create";
    }

    public static class Career
    {
        public const string Read = "career.job-postings.read";
        public const string Write = "career.job-postings.write";
        public const string Stats = "career.reports.read";
    }

    public static class Payment
    {
        public const string Read = "payment.payments.read";
        public const string Write = "payment.payments.write";
    }

    public static class Prediction
    {
        public const string Extract = "prediction.extract";
    }

    public static class Dashboard
    {
        public const string View = "dashboard.view";
    }

    public static class System
    {
        public const string HealthRead = "system.health.read";
        public const string DiagnosticsRead = "system.diagnostics.read";
    }
}
