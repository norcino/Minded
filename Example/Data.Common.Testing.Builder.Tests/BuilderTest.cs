//using Common.Tests;
//using Data.Entity;
//using Microsoft.VisualStudio.TestTools.UnitTesting;
//using System.Linq;

//namespace Data.Common.Testing.Builder.Tests
//{
//    [TestClass]
//    public class BuilderTest
//    {
//        [TestMethod]
//        public void When_invoking_Build_without_parameters_generate_new_entity_with_the_same_type_as_the_generic_specified()
//        {
//            var entity = new Builder<Transaction>().Build();

//            Assert.IsNotNull(entity);
//            Assert.IsInstanceOfType(entity, typeof(Transaction));
//            Assert.IsTrue(entity.CategoryId != 0);
//            Assert.IsTrue(entity.Credit != 0);
//            Assert.IsTrue(entity.Debit != 0);
//            Assert.IsFalse(string.IsNullOrWhiteSpace(entity.Description));
//            Assert.IsTrue(entity.Id != 0);
//            Assert.IsNotNull(entity.Recorded);            
//        }

//        [TestMethod]
//        public void When_invoking_Build_with_action_generate_new_entity_with_specified_property_values()
//        {
//            var expectedId = Any.Int();
//            var expectedRecorded = Any.DateTime();
//            var expectedDescription = Any.String();
//            var expectedCredit = Any.Decimal();
//            var expectedDebit = Any.Decimal();
//            var expectedCategoryId = Any.Int();
//            var expectedCategory = new Builder<Category>().Build();

//            var entity = new Builder<Transaction>()
//                .Build(t => {
//                    t.Recorded = expectedRecorded;
//                    t.Id = expectedId;
//                    t.Description = expectedDescription;
//                    t.Credit = expectedCredit;
//                    t.Debit = expectedDebit;
//                    t.CategoryId = expectedCategoryId;
//                    t.Category = expectedCategory;
//                });

//            Assert.IsNotNull(entity);            
//            Assert.IsInstanceOfType(entity, typeof(Transaction));
//            Assert.AreEqual(expectedId, entity.Id);
//            Assert.AreEqual(expectedRecorded, entity.Recorded);
//            Assert.AreEqual(expectedDescription, entity.Description);
//            Assert.AreEqual(expectedCredit, entity.Credit);
//            Assert.AreEqual(expectedDebit, entity.Debit);
//            Assert.AreEqual(expectedCategoryId, entity.CategoryId);
//            Assert.AreEqual(expectedCategory, entity.Category);
//        }

//        [TestMethod]
//        public void When_invoking_Build_with_action_specifying_one_property_Should_generate_new_entity_with_specified_property_and_all_rest_random_values()
//        {
//            var expectedCategory = new Builder<Category>().Build();
//            var expectedCategoryId = expectedCategory.Id;

//            var entity = new Builder<Transaction>()
//                .Build(t => {
//                    t.CategoryId = expectedCategoryId;
//                    t.Category = expectedCategory;
//                });

//            Assert.IsNotNull(entity);
//            Assert.IsInstanceOfType(entity, typeof(Transaction));

//            // Custom properties specified via Action
//            Assert.AreEqual(expectedCategoryId, entity.CategoryId);
//            Assert.AreEqual(expectedCategory, entity.Category);

//            // Default data must be set
//            Assert.IsTrue(entity.Credit != 0);
//            Assert.IsTrue(entity.Debit != 0);
//            Assert.IsFalse(string.IsNullOrWhiteSpace(entity.Description));
//            Assert.IsTrue(entity.Id != 0);
//            Assert.IsNotNull(entity.Recorded);
//        }

//        [TestMethod]
//        public void When_invoking_BuildMany_passing_the_number_of_desired_entities_and_a_setup_action_Should_return_the_expected_number_of_valid_entities()
//        {
//            const int numberOfDesiredEntities = 123;

//            var builtEntities = Builder<Transaction>.New().BuildMany(numberOfDesiredEntities, (t,i) => {
//                t.Description = $"Desc_{i}";
//                t.Credit = i * 2;
//                t.Debit = i * 3;
//                t.Id = i;
//                t.CategoryId = i + 1;
//            });

//            Assert.AreEqual(numberOfDesiredEntities, builtEntities.Count);

//            var entityIndex = 1;
//            builtEntities.ForEach(e =>
//            {
//                Assert.IsNotNull(e);
//                Assert.IsInstanceOfType(e, typeof(Transaction));
//                Assert.AreEqual($"Desc_{entityIndex}", e.Description);
//                Assert.AreEqual(entityIndex * 2, e.Credit);
//                Assert.AreEqual(entityIndex * 3, e.Debit);
//                Assert.AreEqual(entityIndex, e.Id);
//                Assert.AreEqual(entityIndex + 1, e.CategoryId);
//                entityIndex++;
//            });
//        }

//        [TestMethod]
//        public void When_invoking_BuildMany_passing_the_number_of_desired_entities_without_a_Setup_action_Should_return_the_expected_number_of_entities_with_random_property_values()
//        {
//            const int numberOfDesiredEntities = 123;

//            var builtEntities = Builder<Transaction>.New().BuildMany(numberOfDesiredEntities);

//            Assert.AreEqual(numberOfDesiredEntities, builtEntities.Count);
//            builtEntities.ForEach(e =>
//            {
//                Assert.IsNotNull(e);
//                Assert.IsInstanceOfType(e, typeof(Transaction));
//                Assert.IsTrue(e.CategoryId != 0);
//                Assert.IsTrue(e.Credit != 0);
//                Assert.IsTrue(e.Debit != 0);
//                Assert.IsFalse(string.IsNullOrWhiteSpace(e.Description));
//                Assert.IsTrue(e.Id != 0);
//                Assert.IsNotNull(e.Recorded);
//            });
//        }

//        [TestMethod]
//        public void When_invoking_Build_passing_depth_1_Should_generate_default_valid_Transaction_including_Category_and_CategoryId()
//        {
//            var transaction = new Builder<Transaction>().Build(1);

//            Assert.IsNotNull(transaction);
//            Assert.IsNotNull(transaction.Category);
//            Assert.AreEqual(transaction.CategoryId, transaction.Category.Id);
//        }

//        [TestMethod]
//        public void When_invoking_Build_without_depth_Should_generate_default_valid_Transaction_with_no_child_entities()
//        {
//            var transaction = new Builder<Transaction>().Build();

//            Assert.IsNotNull(transaction);
//            Assert.IsNull(transaction.Category);
//            Assert.IsTrue(transaction.CategoryId > 0);
//            Assert.IsTrue(transaction.Id > 0);
//            Assert.IsTrue(transaction.Credit > 0);
//            Assert.IsTrue(transaction.Debit > 0);
//            Assert.IsTrue(!string.IsNullOrWhiteSpace(transaction.Description));
//        }

//        [TestMethod]
//        public void When_invoking_Build_passing_depth_1_Should_generate_default_valid_Category_with_multiple_transactions()
//        {
//            var category = new Builder<Category>().Build(1);

//            Assert.IsNotNull(category);
//            Assert.IsNotNull(category.Transactions);
//            category.Transactions.ToList().ForEach(t => {
//                Assert.IsNotNull(t);
//                Assert.IsNull(t.Category);
//                Assert.IsTrue(t.CategoryId > 0);
//                Assert.IsTrue(t.Id > 0);
//                Assert.IsTrue(t.Credit > 0);
//                Assert.IsTrue(t.Debit > 0);
//                Assert.IsTrue(!string.IsNullOrWhiteSpace(t.Description));
//            });
//        }

//        [TestMethod]
//        public void When_invoking_Build_passing_depth_2_Should_generate_default_valid_Transaction_CategoryId_Category_and_Transactions_in_Category()
//        {
//            var expectedNumberOfChildEntitiesInCollection = Any.Int(1);
//            var builder = new Builder<Category>
//            {
//                NumberOfNestedEntitiesInCollections = expectedNumberOfChildEntitiesInCollection
//            };

//            var category = builder.Build(1);

//            Assert.IsNotNull(category);
//            Assert.IsNotNull(category.Transactions);
//            Assert.IsNotNull(category.Transactions.Count == expectedNumberOfChildEntitiesInCollection);
//            category.Transactions.ToList().ForEach(t => {
//                Assert.IsNotNull(t);
//                Assert.IsNull(t.Category);
//                Assert.IsTrue(t.CategoryId > 0);
//                Assert.IsTrue(t.Id > 0);
//                Assert.IsTrue(t.Credit > 0);
//                Assert.IsTrue(t.Debit > 0);
//                Assert.IsTrue(!string.IsNullOrWhiteSpace(t.Description));
//            });
//        }
//    }
//}
