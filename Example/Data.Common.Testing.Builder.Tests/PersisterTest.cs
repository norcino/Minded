//using System.Linq;
//using Common.IntegrationTests;
//using Data.Entity;
//using Microsoft.VisualStudio.TestTools.UnitTesting;
//using Common.Tests.FluentAssertion;

//namespace Data.Common.Testing.Builder.Tests
//{
//    [TestClass]
//    public class PersisterTest : BaseIdempotentIntegrationTest
//    {
//        [TestMethod]
//        public void Persist_should_save_entity_to_the_database_with_default_values()
//        {
//            var category = Persister<Category>.New().Persist();
//            var dbCategory = Context.Categories.FirstOrDefault(c => c.Id == category.Id);
//            Assert.That.This(category).HasSameProperties(dbCategory);
//        }

//        [TestMethod]
//        public void Persist_passing_10_should_save_10_entities_to_the_database_with_default_values()
//        {
//            var categories = Persister<Category>.New().Persist(10);
//            Assert.That.All(categories).HaveCount(10);
//            Assert.That.All(categories).Are(c => c != null);

//            categories.ForEach(c =>
//                Assert.That.This(Context.Categories.FirstOrDefault(current => current.Id == c.Id)).HasSameProperties(c)
//            );
//        }
        
//        [TestMethod]
//        public void Persist_should_create_necessary_foreign_key_entities_and_assign_it_to_the_created_entity()
//        {
//            var transaction = Persister<Transaction>.New().Persist();
//            Assert.That.This(transaction).IsNotNull()
//                .And()
//                .Has(t => t.Category != null)
//                .And()
//                .Has(t => t.CategoryId > 0)
//                .And()
//                .Has(t => t.CategoryId == t.Category.Id);
//        }
//    }
//}