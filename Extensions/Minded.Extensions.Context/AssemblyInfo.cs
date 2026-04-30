using System.Runtime.CompilerServices;

/// <summary>
/// Makes internal members visible to the unit test assembly so the decorators and the depth counter
/// can be exercised directly. The public key below matches the strong name defined by key.snk and
/// shared across all Minded assemblies.
/// </summary>
[assembly: InternalsVisibleTo("Minded.Extensions.Context.Tests, PublicKey=0024000004800000940000000602000000240000525341310004000001000100894dcac709653b2c168175a694af3958d0fec0c281756dab28a7c8ad8e5ea8569873fd812c4cf74b3a0e265ac657914e0a9236728f9525c9295992b724ffd13d380e666d0292850f4653d3fd5c3e855e13ed238de8d7c26856a5dd5a936ab05c53f1860384a423079cf6d915ee2868035a2e48f36e86546e8e4a384663e0dcd6")]
