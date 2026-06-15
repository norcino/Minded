[ResourceAuthorize(resurceIdProperty: nameof(ProjectId), resurceIdClaim: Claims.UserId, //identityIdProperty: nameof(UserId), queryType: typeof(CanUserUpdateProjectQuery))]
[ResourceAuthorize(resurceIdProperty: nameof(ProjectId), resurceIdClaim: Claims.UserId, //identityIdProperty: nameof(UserId), queryType: typeof(CanUserUpdateProjectQuery), Or, Permission)]
[ResourceAuthorize(resurceIdProperty: nameof(ProjectId), resurceIdClaim: Claims.UserId, //identityIdProperty: nameof(UserId), queryType: typeof(CanUserUpdateProjectQuery), Or, Role)]
[ResourceAuthorize(resurceIdProperty: nameof(ProjectId), resurceIdClaim: Claims.UserId, //identityIdProperty: nameof(UserId), queryType: typeof(CanUserUpdateProjectQuery), Or, Policy)]
public sealed record UpdateProjectCommand(Guid ProjectId, string Name) : ICommand;

=================================================================================
// Checks that the tenant in the claims matches the resource TenantIdentifier
// but must also have roles editor and supervisor, unless is an Admin
RequiresPolicy(Policies.RestrictTenant, and: [Role.Editor, Role.Supervisor], or: [Role.Admin])

// Checks that the region claim has EU value
RequiresPolicy(Policies.RestrictRegionEU)

// Belongs to group
RequiresPolicy(Policies.RestrictToAssignedGroups, or: [Role.CanAccessAllGroups])

SPEC:
POLICY is evaluated in AND with roles and permissions.
OR indivisual OR roles and permissions are evaluated first, so the policy is not evaluated of OR is true


💡use r_ and p_ prefixes to define roles and permissions.
This way the policy definition can be more flexible as the policy engine can determine if it is one or the other and match it
Role.Supervisor = r_Supervisor
Permission.CanAccessAllGroups = p_CanAccessAllGroups

=================================================================================

1. Policy-based authorization

This means you define a named authorization rule such as "CanApproveInvoice" or "CanManageCustomerData" and evaluate that policy instead of checking roles directly in handlers or controllers.

A policy can combine multiple requirements:

authenticated user
specific claim
specific role
minimum age / region / tenant
custom requirement handler logic
Why use it

It keeps authorization rules centralized and reusable. Instead of scattering if user.IsInRole("Admin") everywhere, you ask for a policy.

Examples

Example 1: Finance approval policy

User must be in role FinanceManager
and must have claim department = finance

A command like ApproveExpenseReportCommand can require policy "ApproveExpense".

Example 2: Export personal data policy

User must have claim scope = gdpr.export
and MFA must be present
and account must not be suspended

A query like ExportCustomerDataQuery can require "ExportPersonalData".

In .NET terms

This is the standard ASP.NET Core authorization model:

AddPolicy(...)
IAuthorizationService
custom AuthorizationHandler<TRequirement>
2. Claim predicates beyond roles and permissions

This means checking claims as data, not just role names or coarse permissions.

A claim can represent:

tenant
region
clearance level
subscription tier
employment type
feature flags
authentication method
customer segment

The important part is the predicate: not just “has claim,” but “claim satisfies condition.”

Why use it

Roles are too broad for many business rules. Claims let you express user context more precisely.

Examples

Example 1: Regional access
A sales rep can query orders only when:

region = EMEA

So GetRegionalOrdersQuery is allowed only if the user’s region claim matches the requested region.

Example 2: Clearance-based access
A support engineer may view incident details only if:

clearance >= 3

That is not a simple role check. It is a predicate on a claim value.

Example 3: Tenant isolation
A user may execute UpdateProjectCommand only if:

tenant_id claim equals the project’s tenant

This is common in SaaS systems.

In practice

You end up writing checks like:

claim exists
claim value matches input
claim value is in allowed set
numeric claim meets threshold
multiple claims must be consistent
3. Ownership or resource-instance checks

This means authorization depends on the specific resource being accessed, not just the user’s general identity.

The question becomes:
“Can this user modify this particular order/document/project?”

This is often called resource-based authorization.

Why use it

Two users with the same role may have different access to the same resource instance.

Examples

Example 1: Document ownership
A user can edit a draft only if:

they created the draft
or they are assigned as editor
or they are an admin

So EditDraftCommand needs access to the draft record and checks draft.OwnerUserId == currentUserId.

Example 2: Manager of owning department
A manager can approve a purchase request only if:

the request belongs to their department

Even if two managers share the same role, each should only act within their own department.

Example 3: Patient record access
A clinician may view a patient record only if:

they are on the care team for that patient

That requires loading the resource and related assignments.

In CQRS / Clean Architecture

This often happens in a command or query pipeline step, or in a domain-facing authorization service:

load resource metadata
evaluate current user against that resource
allow or deny

This is usually more accurate than role checks alone.

4. Dynamic rule evaluation from a database

This means the authorization rules are not fully hardcoded. Some or all conditions come from configuration or rule tables stored in the database.

Typical examples:

per-tenant rules
country-specific rules
thresholds that business admins can change
approval matrices
feature entitlements
temporary exceptions
Why use it

It lets the business change rules without redeploying code.

Examples

Example 1: Approval matrix
A purchase order can be approved based on rules in a table:

up to 1,000: Team Lead
1,001–10,000: Department Head
above 10,000: CFO

Your ApprovePurchaseOrderCommand reads the approval rules from the database and evaluates them dynamically.

Example 2: Country-specific retention/export rules
A DeleteCustomerDataCommand may be allowed in one jurisdiction after 30 days, but another requires 90 days and legal hold checks.

Those rules can come from DB configuration per country or tenant.

Example 3: Tenant-configurable access
Tenant A allows project deletion only for ProjectOwner
Tenant B allows ProjectOwner or PortfolioManager

The engine looks up tenant policy definitions at runtime.