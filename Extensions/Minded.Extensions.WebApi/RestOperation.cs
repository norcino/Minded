using System;

namespace Minded.Extensions.WebApi
{
    /// <summary>
    /// Identifies the logical REST operation being performed by an API endpoint.
    /// The value is used by <see cref="IRulesProcessor"/> and <see cref="IRestRulesProvider"/> to
    /// select the REST rules that should govern the HTTP response.
    /// Multiple values can be combined with bitwise OR when a rule should apply to several operations.
    /// </summary>
    [Flags]
    public enum RestOperation : int
    {
        /// <summary>Matches any operation (wildcard). Use in catch-all rules.</summary>
        Any                     = 0,

        /// <summary>A fire-and-forget action command (no response body expected).</summary>
        Action                  = 1 << 0,
        /// <summary>An action command that returns content in the response body.</summary>
        ActionWithContent       = 1 << 1,
        /// <summary>An action command that returns a typed result in the response body.</summary>
        ActionWithResultContent = 1 << 2,
        /// <summary>Composite flag matching <see cref="Action"/>, <see cref="ActionWithContent"/>, and <see cref="ActionWithResultContent"/>.</summary>
        AnyAction               = Action | ActionWithContent | ActionWithResultContent,

        /// <summary>A create command that produces a resource (HTTP 201 Created).</summary>
        Create                  = 1 << 3,
        /// <summary>A create command that returns the newly created resource in the body.</summary>
        CreateWithContent       = 1 << 4,
        /// <summary>Composite flag matching <see cref="Create"/> and <see cref="CreateWithContent"/>.</summary>
        AnyCreate               = Create | CreateWithContent,

        /// <summary>A delete command.</summary>
        Delete                  = 1 << 5,
        /// <summary>A query that returns a collection of resources.</summary>
        GetMany                 = 1 << 6,
        /// <summary>A query that returns a single resource identified by its key.</summary>
        GetSingle               = 1 << 7,
        /// <summary>Composite flag matching <see cref="GetMany"/> and <see cref="GetSingle"/>.</summary>
        AnyGet                  = GetMany | GetSingle,

        /// <summary>A partial-update command (HTTP PATCH) without a response body.</summary>
        Patch                   = 1 << 8,
        /// <summary>A partial-update command (HTTP PATCH) that returns the patched resource.</summary>
        PatchWithContent        = 1 << 9,
        /// <summary>Composite flag matching <see cref="Patch"/> and <see cref="PatchWithContent"/>.</summary>
        AnyPatch                = Patch | PatchWithContent,

        /// <summary>A full-update command (HTTP PUT) without a response body.</summary>
        Update                  = 1 << 10,
        /// <summary>A full-update command (HTTP PUT) that returns the updated resource.</summary>
        UpdateWithContent       = 1 << 11,
        /// <summary>Composite flag matching <see cref="Update"/> and <see cref="UpdateWithContent"/>.</summary>
        AnyUpdate               = Update | UpdateWithContent
    }

    /// <summary>Extension methods for <see cref="RestOperation"/>.</summary>
    public static class RestOperationExtensions
    {
        /// <summary>
        /// Returns <c>true</c> when <paramref name="operation"/> contains <paramref name="flag"/>
        /// or when <paramref name="flag"/> is <see cref="RestOperation.Any"/> (wildcard).
        /// </summary>
        /// <param name="operation">The operation to test.</param>
        /// <param name="flag">The flag or composite flag to check for.</param>
        public static bool Matches(this RestOperation operation, RestOperation flag)
        {
            return (operation & flag) != 0 || flag == RestOperation.Any;
        }
    }
}
