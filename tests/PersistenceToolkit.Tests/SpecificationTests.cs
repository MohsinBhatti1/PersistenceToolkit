using PersistanceToolkit.Abstractions.Specifications;
using PersistanceToolkit.Tests.Initializers;
using Xunit;

namespace PersistanceToolkit.Tests
{
    public class SpecificationTests : IClassFixture<PTKTestFixture>
    {
        private readonly PTKTestFixture _fixture;
        public SpecificationTests(PTKTestFixture fixture)
        {
            _fixture = fixture;
        }
        [Fact]
        public async Task TestPagination()
        {
            try
            {
                var result = await _fixture.ParentRepository.SaveRange(GetParentObjects());

                var spec = new ParentSpec();
                var paginatedData = await _fixture.ParentRepository.PaginatedListAsync(spec, 980, 100);
                var allData = await _fixture.ParentRepository.ListAsync(spec);
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message);
            }
            Assert.Equal(1, 1);
        }
        private static List<Parent> GetParentObjects()
        {
            List<Parent> parents = new List<Parent>();
            for (int i = 0; i < 1000; i++)
            {
                parents.Add(new Parent
                {
                    Title = $"Parent {i}",
                    Children = new List<Child>
                    {
                        new Child() { Title = $"Child {i}-1" },
                        new Child() { Title = $"Child {i}-2" }
                    }
                });
            }
            return parents;
        }
    }
}
