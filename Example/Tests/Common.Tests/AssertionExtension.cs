using System.Net;
using System.Net.Http;
using FluentAssertions.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Common.Tests.FluentAssertion
{
    /// <summary>
    /// Add assertion extensions
    /// </summary>
    public static class AssertFluentExtension
    {
        #region HttpResponse Status Codes
        public static void IsOkHttpResponse(this Assert assert, HttpResponseMessage response)
        {
            Assert.IsNotNull(response);
            if (response.StatusCode != HttpStatusCode.OK)
                throw new AssertFailedException($"Expected Ok (200) status code, but was {response.StatusCode} ({(int)response.StatusCode})");
        }

        public static void IsCreatedHttpResponse(this Assert assert, HttpResponseMessage response)
        {
            Assert.IsNotNull(response);
            if (response.StatusCode != HttpStatusCode.Created)
                throw new AssertFailedException($"Expected Created (201) status code, but was {response.StatusCode} ({(int)response.StatusCode})");
        }

        public static void IsNotFoundHttpResponse(this Assert assert, HttpResponseMessage response)
        {
            Assert.IsNotNull(response);
            if (response.StatusCode != HttpStatusCode.NotFound)
                throw new AssertFailedException($"Expected NotFound (404) status code, but was {response.StatusCode} ({(int)response.StatusCode})");
        }

        public static void IsBadRequestHttpResponse(this Assert assert, HttpResponseMessage response)
        {
            Assert.IsNotNull(response);
            if (response.StatusCode != HttpStatusCode.BadRequest)
                throw new AssertFailedException($"Expected BadRequest (400) status code, but was {response.StatusCode} ({(int)response.StatusCode})");
        }

        public static void HttpResponseStatusCodeIs(this Assert assert, HttpResponseMessage response, HttpStatusCode statuscode)
        {
            Assert.IsNotNull(response);
            if (response.StatusCode != statuscode)
                throw new AssertFailedException($"Expected {statuscode} ({(int)statuscode}) status code, but was {response.StatusCode} ({(int)response.StatusCode})");
        }
        #endregion
    }
}
