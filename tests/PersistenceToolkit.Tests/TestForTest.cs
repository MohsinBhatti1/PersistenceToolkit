using PersistanceToolkit.Domain;
using PersistanceToolkit.Tests.Initializers;
using System.Collections.Generic;
using Xunit;

namespace PersistanceToolkit.Tests
{
    public class TestForTest : IClassFixture<PTKTestFixture>
    {
        private readonly PTKTestFixture _fixture;
        public TestForTest(PTKTestFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task TestToTest()
        {
            try
            {

                var user = new User { FirstName = "Mohsin", LastName = "Naeem" };
                await _fixture.UserRepository.Save(user);

                Parent parent = GetParentObj();
                await _fixture.ParentRepository.Save(parent);

                var spec = new ParentSpec();
                var result = await _fixture.ParentRepository.FirstOrDefaultAsync(spec);
                result.Children.First().Title = "124";
                await _fixture.ParentRepository.Save(result);
                var obj = result.DeepClone();
                var obj1 = obj.GetJson().GetObject<Parent>();

            }
            catch (Exception ex)
            {
                // Handle exception
                Assert.Fail(ex.Message);
            }
            Assert.Equal(1, 1);
        }
        [Fact]
        public async Task TestToTest1()
        {
            try
            {

                var user = new User { FirstName = "Mohsin", LastName = "Naeem" };
                await _fixture.UserRepository.Save(user);

                Parent parent = GetParentObj();
                await _fixture.ParentRepository.Save(parent);

                var spec = new ParentSpec();
                var result = await _fixture.ParentRepository.FirstOrDefaultAsync(spec);
                result.Children.First().Title = "124";
                await _fixture.ParentRepository.Save(result);
                var obj = result.DeepClone();
                var obj1 = obj.GetJson().GetObject<Parent>();

            }
            catch (Exception ex)
            {
                // Handle exception
                Assert.Fail(ex.Message);
            }
            Assert.Equal(1, 1);
        }

        private static Parent GetParentObj()
        {
            return new Parent
            {
                Title = "123",
                Children = new List<Child> { new Child() { ParentId = 1, Title = "456" }, new Child() { ParentId = 1, Title = "789" } },
                User = new User { FirstName = "Mohsin123", LastName = "Naeem" }
            };
        }
 
    }
}
