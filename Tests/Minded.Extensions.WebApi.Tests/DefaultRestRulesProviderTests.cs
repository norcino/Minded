using System.Net;
using Minded.Extensions.WebApi;

[TestClass]
public class DefaultRestRulesProviderTests
{
    private DefaultRestRulesProvider _provider;

    [TestInitialize]
    public void Setup()
    {
        _provider = new DefaultRestRulesProvider();
    }

    [TestMethod]
    public void GetQueryRules_WhenCalled_ReturnsExpectedRules()
    {
        var rules = _provider.GetQueryRules().ToList();

        Assert.AreEqual(6, rules.Count);
        // NotAuthorizedQuery
        Assert.IsTrue(rules.Any(r => r.Operation == RestOperation.Any && r.ResultStatusCode == HttpStatusCode.Unauthorized && r.ContentResponse == ContentResponse.Full));
        // NotAuthenticatedQuery
        Assert.IsTrue(rules.Any(r => r.Operation == RestOperation.Any && r.ResultStatusCode == HttpStatusCode.Forbidden && r.ContentResponse == ContentResponse.Full));
        // GetSingleSuccessfully
        Assert.IsTrue(rules.Any(r => r.Operation == RestOperation.GetSingle && r.ResultStatusCode == HttpStatusCode.OK && r.ContentResponse == ContentResponse.Result));
        // GetManySuccessfully
        Assert.IsTrue(rules.Any(r => r.Operation == RestOperation.GetMany && r.ResultStatusCode == HttpStatusCode.OK && r.ContentResponse == ContentResponse.Result));
        // GetSingleUnsuccessfully
        Assert.IsTrue(rules.Any(r => r.Operation == RestOperation.GetSingle && r.ResultStatusCode == HttpStatusCode.NotFound && r.ContentResponse == ContentResponse.Full));
        // GetInvalid
        Assert.IsTrue(rules.Any(r => r.Operation == RestOperation.AnyGet && r.ResultStatusCode == HttpStatusCode.BadRequest && r.ContentResponse == ContentResponse.Full));
    }

    [TestMethod]
    public void GetCommandRules_WhenCalled_ReturnsExpectedRules()
    {
        var rules = _provider.GetCommandRules().ToList();

        Assert.AreEqual(25, rules.Count);
        Assert.IsTrue(rules.Any(r => r.Operation == RestOperation.Create && r.ResultStatusCode == HttpStatusCode.Created && r.ContentResponse == ContentResponse.None)); // CreateSuccessfully
        Assert.IsTrue(rules.Any(r => r.Operation == RestOperation.CreateWithContent && r.ResultStatusCode == HttpStatusCode.Created && r.ContentResponse == ContentResponse.Result)); // CreateWithContentSuccessfully
        Assert.IsTrue(rules.Any(r => r.Operation == RestOperation.CreateWithContent && r.ResultStatusCode == HttpStatusCode.BadRequest && r.ContentResponse == ContentResponse.Full)); // CreateWithContentInvalid
        Assert.IsTrue(rules.Any(r => r.Operation == RestOperation.Create && r.ResultStatusCode == HttpStatusCode.BadRequest && r.ContentResponse == ContentResponse.Full)); // CreateInvalid
        Assert.IsTrue(rules.Any(r => r.Operation == RestOperation.UpdateWithContent && r.ResultStatusCode == HttpStatusCode.OK && r.ContentResponse == ContentResponse.Result)); // UpdateWithContentSuccessfully
        Assert.IsTrue(rules.Any(r => r.Operation == RestOperation.Update && r.ResultStatusCode == HttpStatusCode.NoContent && r.ContentResponse == ContentResponse.None)); // UpdateSuccessfully
        Assert.IsTrue(rules.Any(r => r.Operation == RestOperation.Update && r.ResultStatusCode == HttpStatusCode.NotFound && r.ContentResponse == ContentResponse.None)); // UpdateNotfound
        Assert.IsTrue(rules.Any(r => r.Operation == RestOperation.UpdateWithContent && r.ResultStatusCode == HttpStatusCode.NotFound && r.ContentResponse == ContentResponse.Full)); // UpdateWithContentNotfound
        Assert.IsTrue(rules.Any(r => r.Operation == RestOperation.Update && r.ResultStatusCode == HttpStatusCode.BadRequest && r.ContentResponse == ContentResponse.None)); // UpdateInvalid
        Assert.IsTrue(rules.Any(r => r.Operation == RestOperation.UpdateWithContent && r.ResultStatusCode == HttpStatusCode.BadRequest && r.ContentResponse == ContentResponse.Full)); // UpdateWithContentInvalid
        Assert.IsTrue(rules.Any(r => r.Operation == RestOperation.PatchWithContent && r.ResultStatusCode == HttpStatusCode.OK && r.ContentResponse == ContentResponse.Result)); // PatchWithContentSuccessfully
        Assert.IsTrue(rules.Any(r => r.Operation == RestOperation.Patch && r.ResultStatusCode == HttpStatusCode.NoContent && r.ContentResponse == ContentResponse.None)); // PatchSuccessfully
        Assert.IsTrue(rules.Any(r => r.Operation == RestOperation.PatchWithContent && r.ResultStatusCode == HttpStatusCode.NotFound && r.ContentResponse == ContentResponse.Full)); // PatchWithContentNotfound
        Assert.IsTrue(rules.Any(r => r.Operation == RestOperation.Patch && r.ResultStatusCode == HttpStatusCode.NotFound && r.ContentResponse == ContentResponse.None)); // PatchNotfound
        Assert.IsTrue(rules.Any(r => r.Operation == RestOperation.PatchWithContent && r.ResultStatusCode == HttpStatusCode.BadRequest && r.ContentResponse == ContentResponse.Full)); // PatchWithContentInvalid
        Assert.IsTrue(rules.Any(r => r.Operation == RestOperation.Patch && r.ResultStatusCode == HttpStatusCode.BadRequest && r.ContentResponse == ContentResponse.None)); // PatchInvalid
        Assert.IsTrue(rules.Any(r => r.Operation == RestOperation.Delete && r.ResultStatusCode == HttpStatusCode.OK && r.ContentResponse == ContentResponse.None)); // DeleteSuccessfully
        Assert.IsTrue(rules.Any(r => r.Operation == RestOperation.Delete && r.ResultStatusCode == HttpStatusCode.NotFound && r.ContentResponse == ContentResponse.None)); // DeleteNotfound
        Assert.IsTrue(rules.Any(r => r.Operation == RestOperation.ActionWithContent && r.ResultStatusCode == HttpStatusCode.OK && r.ContentResponse == ContentResponse.Full)); // PostWithContentSuccessfully
    }
}
