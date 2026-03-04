using Microsoft.Extensions.DependencyInjection;
using PersistenceToolkit.Abstractions.Repositories;
using PersistenceToolkit.Abstractions.Specifications;
using PersistenceToolkit.Tests.Initializers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using System;
using PersistenceToolkit.Tests.Entities;

namespace PersistenceToolkit.Tests
{
    public class SpecificationPostProcessingTests : IDisposable
    {
        private readonly IAggregateRepository<Parent> _parentTableRepository;
        private readonly ServiceProvider _serviceProvider;

        public SpecificationPostProcessingTests()
        {
            _serviceProvider = DependencyInjectionSetup.InitializeServiceProvider();
            _parentTableRepository = _serviceProvider.GetService<IAggregateRepository<Parent>>();
        }

        public void Dispose()
        {
            _serviceProvider?.Dispose();
        }

        /// <summary>
        /// Verifies that FirstOrDefaultAsync with specification calls PostProcessingAction.
        /// </summary>
        [Fact]
        public async Task FirstOrDefaultAsync_With_Specification_Should_Call_PostProcessingAction()
        {
            // Arrange
            var entity = new Parent { Title = "FirstOrDefaultTest" };
            await _parentTableRepository.Save(entity);

            // Act
            var spec = new ParentSpec();
            var result = await _parentTableRepository.FirstOrDefaultAsync(spec);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.HasChange()); // PostProcessingAction should capture snapshot
        }

        /// <summary>
        /// Verifies that SingleOrDefaultAsync with specification calls PostProcessingAction.
        /// </summary>
        [Fact]
        public async Task SingleOrDefaultAsync_With_Specification_Should_Call_PostProcessingAction()
        {
            // Arrange
            var entity = new Parent { Title = "SingleOrDefaultTest" };
            await _parentTableRepository.Save(entity);

            // Act
            var spec = new ParentSpec();
            var result = await _parentTableRepository.SingleOrDefaultAsync(spec);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.HasChange()); // PostProcessingAction should capture snapshot
        }

        /// <summary>
        /// Verifies that ListAsync with specification calls PostProcessingAction.
        /// </summary>
        [Fact]
        public async Task ListAsync_With_Specification_Should_Call_PostProcessingAction()
        {
            // Arrange
            var entities = new List<Parent>
            {
                new Parent { Title = "ListTest1" },
                new Parent { Title = "ListTest2" },
                new Parent { Title = "ListTest3" }
            };
            await _parentTableRepository.SaveRange(entities);

            // Act
            var spec = new ParentSpec();
            var result = await _parentTableRepository.ListAsync(spec);

            // Assert
            Assert.Equal(3, result.Count);
            Assert.All(result, entity => Assert.False(entity.HasChange())); // All should have snapshots captured
        }

        /// <summary>
        /// Verifies that PaginatedListAsync with specification calls PostProcessingAction.
        /// </summary>
        [Fact]
        public async Task PaginatedListAsync_With_Specification_Should_Call_PostProcessingAction()
        {
            // Arrange
            var entities = new List<Parent>();
            for (int i = 0; i < 10; i++)
            {
                entities.Add(new Parent { Title = $"PaginatedTest{i}" });
            }
            await _parentTableRepository.SaveRange(entities);

            // Act
            var spec = new ParentSpec();
            var result = await _parentTableRepository.PaginatedListAsync(spec, 0, 5);

            // Assert
            Assert.Equal(5, result.Items.Count);
            Assert.Equal(10, result.TotalCount);
            Assert.All(result.Items, entity => Assert.False(entity.HasChange())); // All should have snapshots captured
        }

        /// <summary>
        /// Verifies that CountAsync with specification calls PostProcessingAction.
        /// </summary>
        [Fact]
        public async Task CountAsync_With_Specification_Should_Call_PostProcessingAction()
        {
            // Arrange
            var entities = new List<Parent>
            {
                new Parent { Title = "CountTest1" },
                new Parent { Title = "CountTest2" },
                new Parent { Title = "CountTest3" },
                new Parent { Title = "CountTest4" }
            };
            await _parentTableRepository.SaveRange(entities);

            // Act
            var spec = new ParentSpec();
            var result = await _parentTableRepository.CountAsync(spec);

            // Assert
            Assert.Equal(4, result);
        }

        /// <summary>
        /// Verifies that AnyAsync with specification calls PostProcessingAction.
        /// </summary>
        [Fact]
        public async Task AnyAsync_With_Specification_Should_Call_PostProcessingAction()
        {
            // Arrange
            var entity = new Parent { Title = "AnyTest" };
            await _parentTableRepository.Save(entity);

            // Act
            var spec = new ParentSpec();
            var result = await _parentTableRepository.AnyAsync(spec);

            // Assert
            Assert.True(result);
        }

        /// <summary>
        /// Verifies that PostProcessingAction removes deleted entries from aggregates.
        /// </summary>
        [Fact]
        public async Task PostProcessingAction_Should_Remove_Deleted_Entries_From_Aggregates()
        {
            // Arrange
            var parent = new Parent { Title = "DeletedEntriesTest" };
            var activeChild = new Child { Title = "ActiveChild" };
            var deletedChild = new Child { Title = "DeletedChild" };
            deletedChild.MarkAsDeleted(1, DateTime.Now);
            
            parent.Children = new List<Child> { activeChild, deletedChild };
            await _parentTableRepository.Save(parent);

            // Act
            var spec = new ParentSpec();
            var result = await _parentTableRepository.FirstOrDefaultAsync(spec);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Children);
            Assert.Single(result.Children); // Only active child should remain
            Assert.Equal("ActiveChild", result.Children.First().Title);
            Assert.DoesNotContain(result.Children, c => c.IsDeleted);
        }

        /// <summary>
        /// Verifies that PostProcessingAction removes deleted entries from nested grandchildren.
        /// </summary>
        [Fact]
        public async Task PostProcessingAction_Should_Remove_Deleted_Grandchildren_From_Aggregates()
        {
            // Arrange
            var parent = new Parent { Title = "DeletedGrandchildrenTest" };
            var child = new Child { Title = "Child" };
            var activeGrandChild = new GrandChild { Title = "ActiveGrandChild" };
            var deletedGrandChild = new GrandChild { Title = "DeletedGrandChild" };
            deletedGrandChild.MarkAsDeleted(1, DateTime.Now);
            
            child.GrandChildren = new List<GrandChild> { activeGrandChild, deletedGrandChild };
            parent.Children = new List<Child> { child };
            await _parentTableRepository.Save(parent);

            // Act
            var spec = new ParentSpec();
            var result = await _parentTableRepository.FirstOrDefaultAsync(spec);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Children);
            Assert.Single(result.Children);
            Assert.NotNull(result.Children.First().GrandChildren);
            Assert.Single(result.Children.First().GrandChildren); // Only active grandchild should remain
            Assert.Equal("ActiveGrandChild", result.Children.First().GrandChildren.First().Title);
            Assert.DoesNotContain(result.Children.First().GrandChildren, gc => gc.IsDeleted);
        }

        /// <summary>
        /// Verifies that PostProcessingAction captures snapshots for all entities in result.
        /// </summary>
        [Fact]
        public async Task PostProcessingAction_Should_Capture_Snapshots_For_All_Entities()
        {
            // Arrange
            var entities = new List<Parent>
            {
                new Parent { Title = "SnapshotTest1" },
                new Parent { Title = "SnapshotTest2" },
                new Parent { Title = "SnapshotTest3" }
            };
            await _parentTableRepository.SaveRange(entities);

            // Act
            var spec = new ParentSpec();
            var result = await _parentTableRepository.ListAsync(spec);

            // Assert
            Assert.Equal(3, result.Count);
            Assert.All(result, entity => Assert.False(entity.HasChange())); // All should have snapshots
        }

        /// <summary>
        /// Verifies that PostProcessingAction works with nested deleted entities.
        /// </summary>
        [Fact]
        public async Task PostProcessingAction_Should_Handle_Nested_Deleted_Entities()
        {
            // Arrange
            var parent = new Parent { Title = "NestedDeletedTest" };
            var child1 = new Child { Title = "Child1" };
            var child2 = new Child { Title = "Child2" };
            var child3 = new Child { Title = "Child3" };
            
            child2.MarkAsDeleted(1, DateTime.Now);
            
            parent.Children = new List<Child> { child1, child2, child3 };
            await _parentTableRepository.Save(parent);

            // Act
            var spec = new ParentSpec();
            var result = await _parentTableRepository.FirstOrDefaultAsync(spec);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Children);
            Assert.Equal(2, result.Children.Count); // Only active children should remain
            Assert.Contains(result.Children, c => c.Title == "Child1");
            Assert.Contains(result.Children, c => c.Title == "Child3");
            Assert.DoesNotContain(result.Children, c => c.Title == "Child2");
        }

        /// <summary>
        /// Verifies that PostProcessingAction works with complex nested deleted entities including grandchildren.
        /// </summary>
        [Fact]
        public async Task PostProcessingAction_Should_Handle_Complex_Nested_Deleted_Entities()
        {
            // Arrange
            var parent = new Parent { Title = "ComplexNestedDeletedTest" };
            
            var child1 = new Child { Title = "Child1" };
            var grandChild1 = new GrandChild { Title = "GrandChild1" };
            var grandChild2 = new GrandChild { Title = "GrandChild2" };
            grandChild2.MarkAsDeleted(1, DateTime.Now);
            child1.GrandChildren = new List<GrandChild> { grandChild1, grandChild2 };
            
            var child2 = new Child { Title = "Child2" };
            child2.MarkAsDeleted(1, DateTime.Now);
            
            var child3 = new Child { Title = "Child3" };
            var grandChild3 = new GrandChild { Title = "GrandChild3" };
            child3.GrandChildren = new List<GrandChild> { grandChild3 };
            
            parent.Children = new List<Child> { child1, child2, child3 };
            await _parentTableRepository.Save(parent);

            // Act
            var spec = new ParentSpec();
            var result = await _parentTableRepository.FirstOrDefaultAsync(spec);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Children);
            Assert.Equal(2, result.Children.Count); // Only active children should remain
            
            var activeChild1 = result.Children.First(c => c.Title == "Child1");
            var activeChild3 = result.Children.First(c => c.Title == "Child3");
            
            Assert.Single(activeChild1.GrandChildren); // Only active grandchild should remain
            Assert.Equal("GrandChild1", activeChild1.GrandChildren.First().Title);
            
            Assert.Single(activeChild3.GrandChildren);
            Assert.Equal("GrandChild3", activeChild3.GrandChildren.First().Title);
            
            Assert.DoesNotContain(result.Children, c => c.Title == "Child2");
        }

        /// <summary>
        /// Verifies that PostProcessingAction works when IncludeDeletedRecords is true.
        /// </summary>
        [Fact]
        public async Task PostProcessingAction_Should_Include_Deleted_Records_When_Requested()
        {
            // Arrange
            var parent = new Parent { Title = "IncludeDeletedTest" };
            var activeChild = new Child { Title = "ActiveChild" };
            var deletedChild = new Child { Title = "DeletedChild" };
            deletedChild.MarkAsDeleted(1, DateTime.Now);
            
            parent.Children = new List<Child> { activeChild, deletedChild };
            await _parentTableRepository.Save(parent);

            // Act
            var spec = new ParentSpec { IncludeDeletedRecords = true };
            var result = await _parentTableRepository.FirstOrDefaultAsync(spec);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Children);
            Assert.Equal(2, result.Children.Count); // Both should remain
            Assert.Contains(result.Children, c => c.Title == "ActiveChild");
            Assert.Contains(result.Children, c => c.Title == "DeletedChild" && c.IsDeleted);
        }

        // Custom specifications for testing
        public class ParentSpec : EntitySpecification<Parent>
        {
            public ParentSpec()
            {
                Query.Include(c => c.Children)
                     .ThenInclude(child => child.GrandChildren);
            }
        }
    }
} 