using System;
using FsCheck;
using FsCheck.Fluent;
using FluentAssertions;
using Minded.Extensions.Authorization.Configuration;

namespace Minded.Extensions.Authorization.Tests.Configuration;

[TestClass]
public class AuthorizationOptionsTests
{
    /// <summary>
    /// Property 23: AuthorizationOptions GetEffective resolves provider or falls back —
    /// For any AuthorizationOptions instance, GetEffectiveRequireAuthenticationForAllCommands()
    /// and GetEffectiveRequireAuthenticationForAllQueries() SHALL return the provider's result
    /// when a provider Func is set, and fall back to the static property value when the provider is null.
    /// **Validates: Requirements 24.4**
    /// </summary>
    [TestMethod]
    public void Property23_GetEffective_ResolvesProviderOrFallsBack_Commands()
    {
        var arb = (from staticValue in Gen.Elements(true, false)
                   from providerValue in Gen.Elements<bool?>(true, false, null)
                   select (staticValue, providerValue)).ToArbitrary();

        Prop.ForAll(arb, tuple =>
        {
            var (staticValue, providerValue) = tuple;
            var options = new AuthorizationOptions
            {
                RequireAuthenticationForAllCommands = staticValue
            };

            if (providerValue.HasValue)
            {
                var captured = providerValue.Value;
                options.RequireAuthenticationForAllCommandsProvider = () => captured;
            }

            var effective = options.GetEffectiveRequireAuthenticationForAllCommands();

            if (providerValue.HasValue)
            {
                return (effective == providerValue.Value).ToProperty();
            }
            else
            {
                return (effective == staticValue).ToProperty();
            }
        }).QuickCheckThrowOnFailure();
    }

    /// <summary>
    /// Property 23: AuthorizationOptions GetEffective resolves provider or falls back (Queries).
    /// **Validates: Requirements 24.4**
    /// </summary>
    [TestMethod]
    public void Property23_GetEffective_ResolvesProviderOrFallsBack_Queries()
    {
        var arb = (from staticValue in Gen.Elements(true, false)
                   from providerValue in Gen.Elements<bool?>(true, false, null)
                   select (staticValue, providerValue)).ToArbitrary();

        Prop.ForAll(arb, tuple =>
        {
            var (staticValue, providerValue) = tuple;
            var options = new AuthorizationOptions
            {
                RequireAuthenticationForAllQueries = staticValue
            };

            if (providerValue.HasValue)
            {
                var captured = providerValue.Value;
                options.RequireAuthenticationForAllQueriesProvider = () => captured;
            }

            var effective = options.GetEffectiveRequireAuthenticationForAllQueries();

            if (providerValue.HasValue)
            {
                return (effective == providerValue.Value).ToProperty();
            }
            else
            {
                return (effective == staticValue).ToProperty();
            }
        }).QuickCheckThrowOnFailure();
    }
}
