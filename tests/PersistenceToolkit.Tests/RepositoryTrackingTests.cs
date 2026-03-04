using Microsoft.EntityFrameworkCore;
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
using PersistenceToolkit.Tests.Specifications;
using PersistenceToolkit.Persistence.Persistence;

namespace PersistenceToolkit.Tests
{
    public class RepositoryTrackingTests : IDisposable
    {
        private readonly IAggregateRepository<Parent> _parentTableRepository;
        private readonly ServiceProvider _serviceProvider;

        public RepositoryTrackingTests()
        {
            _serviceProvider = DependencyInjectionSetup.InitializeServiceProvider();
            _parentTableRepository = _serviceProvider.GetService<IAggregateRepository<Parent>>();
        }

        public void Dispose()
        {
            _serviceProvider?.Dispose();
        }

        /// <summary>
        /// Verifies that specification queries use AsNoTracking by default.
        /// This prevents entities from being tracked by the ChangeTracker.
        /// </summary>
        [Fact]
        public async Task Specification_Queries_Should_Use_AsNoTracking()
        {
            // Arrange
            var entity = new Parent { Title = "TrackingTest" };
            await _parentTableRepository.Save(entity);

            // Act
            var spec = new ParentSpec();
            var result = await _parentTableRepository.ListAsync(spec);
            var loadedEntity = result.FirstOrDefault();

            // Assert
            Assert.NotNull(loadedEntity);

            
            // Verify the entity is not tracked by checking its EntityState
            var context = _parentTableRepository.GetType()
                .GetProperty("DbContext", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(_parentTableRepository) as BaseContext;
            
            Assert.NotNull(context);
            var entry = context.Entry(loadedEntity);
            Assert.Equal(EntityState.Detached, entry.State);
        }

        /// <summary>
        /// Verifies that entities are detached after SaveChangesAsync is called.
        /// This prevents memory leaks and ensures clean state for subsequent operations.
        /// </summary>
        [Fact]
        public async Task Entities_Should_Be_Detached_After_SaveChanges()
        {
            // Arrange
            var entity = new Parent { Title = "DetachTest" };

            // Act
            var result = await _parentTableRepository.Save(entity);

            // Assert
            Assert.True(result);
            
            // Verify the entity is detached after save
            var context = _parentTableRepository.GetType()
                .GetProperty("DbContext", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(_parentTableRepository) as BaseContext;
            
            Assert.NotNull(context);
            var entry = context.Entry(entity);
            Assert.Equal(EntityState.Detached, entry.State);
        }

        /// <summary>
        /// Verifies that multiple entities are detached after SaveRange operation.
        /// </summary>
        [Fact]
        public async Task Multiple_Entities_Should_Be_Detached_After_SaveRange()
        {
            // Arrange
            var entities = new List<Parent>
            {
                new Parent { Title = "Entity1" },
                new Parent { Title = "Entity2" },
                new Parent { Title = "Entity3" }
            };

            // Act
            var result = await _parentTableRepository.SaveRange(entities);

            // Assert
            Assert.True(result);
            
            // Verify all entities are detached
            var context = _parentTableRepository.GetType()
                .GetProperty("DbContext", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(_parentTableRepository) as BaseContext;
            
            Assert.NotNull(context);
            
            foreach (var entity in entities)
            {
                var entry = context.Entry(entity);
                Assert.Equal(EntityState.Detached, entry.State);
            }
        }

        /// <summary>
        /// Verifies that entities are detached after delete operations.
        /// </summary>
        [Fact]
        public async Task Entities_Should_Be_Detached_After_Delete()
        {
            // Arrange
            var entity = new Parent { Title = "DeleteTest" };
            await _parentTableRepository.Save(entity);

            // Act
            var result = await _parentTableRepository.DeleteAsync(entity);

            // Assert
            Assert.True(result);
            
            // Verify the entity is detached after delete
            var context = _parentTableRepository.GetType()
                .GetProperty("DbContext", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(_parentTableRepository) as BaseContext;
            
            Assert.NotNull(context);
            var entry = context.Entry(entity);
            Assert.Equal(EntityState.Detached, entry.State);
        }

        /// <summary>
        /// Verifies that entities are detached after delete range operations.
        /// </summary>
        [Fact]
        public async Task Entities_Should_Be_Detached_After_DeleteRange()
        {
            // Arrange
            var entities = new List<Parent>
            {
                new Parent { Title = "DeleteRange1" },
                new Parent { Title = "DeleteRange2" }
            };
            await _parentTableRepository.SaveRange(entities);

            // Act
            var result = await _parentTableRepository.DeleteRangeAsync(entities);

            // Assert
            Assert.True(result);
            
            // Verify all entities are detached
            var context = _parentTableRepository.GetType()
                .GetProperty("DbContext", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(_parentTableRepository) as BaseContext;
            
            Assert.NotNull(context);
            
            foreach (var entity in entities)
            {
                var entry = context.Entry(entity);
                Assert.Equal(EntityState.Detached, entry.State);
            }
        }

        /// <summary>
        /// Verifies that entities are detached after delete by specification.
        /// </summary>
        [Fact]
        public async Task Entities_Should_Be_Detached_After_DeleteBySpecification()
        {
            // Arrange
            var entities = new List<Parent>
            {
                new Parent { Title = "DeleteSpec1" },
                new Parent { Title = "DeleteSpec2" }
            };
            await _parentTableRepository.SaveRange(entities);

            var spec = new CustomTitleSpec("DeleteSpec1");

            // Act
            var result = await _parentTableRepository.DeleteRangeAsync(spec);

            // Assert
            Assert.True(result);
            
            // Verify entities are detached
            var context = _parentTableRepository.GetType()
                .GetProperty("DbContext", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(_parentTableRepository) as BaseContext;
            
            Assert.NotNull(context);
            
            foreach (var entity in entities)
            {
                var entry = context.Entry(entity);
                Assert.Equal(EntityState.Detached, entry.State);
            }
        }

        /// <summary>
        /// Verifies that ChangeTracker is cleared after SaveChangesAsync.
        /// This ensures no entities remain tracked in memory.
        /// </summary>
        [Fact]
        public async Task ChangeTracker_Should_Be_Cleared_After_SaveChanges()
        {
            // Arrange
            var entity = new Parent { Title = "ChangeTrackerTest" };

            // Act
            await _parentTableRepository.Save(entity);

            // Assert
            var context = _parentTableRepository.GetType()
                .GetProperty("DbContext", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(_parentTableRepository) as BaseContext;
            
            Assert.NotNull(context);
            Assert.Equal(0, context.ChangeTracker.Entries().Count());
        }

        /// <summary>
        /// Verifies that entities with navigation properties are also detached.
        /// </summary>
        [Fact]
        public async Task Entities_With_Navigation_Properties_Should_Be_Detached()
        {
            // Arrange
            var parent = new Parent
            {
                Title = "ParentWithChildren",
                Children = new List<Child>
                {
                    new Child { Title = "Child1" },
                    new Child { Title = "Child2" }
                }
            };

            // Act
            var result = await _parentTableRepository.Save(parent);

            // Assert
            Assert.True(result);
            
            var context = _parentTableRepository.GetType()
                .GetProperty("DbContext", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(_parentTableRepository) as BaseContext;
            
            Assert.NotNull(context);
            
            // Verify parent is detached
            var parentEntry = context.Entry(parent);
            Assert.Equal(EntityState.Detached, parentEntry.State);
            
            // Verify children are detached
            foreach (var child in parent.Children)
            {
                var childEntry = context.Entry(child);
                Assert.Equal(EntityState.Detached, childEntry.State);
            }
        }

        /// <summary>
        /// Verifies that entities with nested grandchildren are also detached.
        /// </summary>
        [Fact]
        public async Task Entities_With_Grandchildren_Should_Be_Detached()
        {
            // Arrange
            var parent = new Parent
            {
                Title = "ParentWithGrandchildren",
                Children = new List<Child>
                {
                    new Child 
                    { 
                        Title = "Child1",
                        GrandChildren = new List<GrandChild>
                        {
                            new GrandChild { Title = "GrandChild1" },
                            new GrandChild { Title = "GrandChild2" }
                        }
                    },
                    new Child 
                    { 
                        Title = "Child2",
                        GrandChildren = new List<GrandChild>
                        {
                            new GrandChild { Title = "GrandChild3" }
                        }
                    }
                }
            };

            // Act
            var result = await _parentTableRepository.Save(parent);

            // Assert
            Assert.True(result);
            
            var context = _parentTableRepository.GetType()
                .GetProperty("DbContext", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(_parentTableRepository) as BaseContext;
            
            Assert.NotNull(context);
            
            // Verify parent is detached
            var parentEntry = context.Entry(parent);
            Assert.Equal(EntityState.Detached, parentEntry.State);
            
            // Verify children are detached
            foreach (var child in parent.Children)
            {
                var childEntry = context.Entry(child);
                Assert.Equal(EntityState.Detached, childEntry.State);
                
                // Verify grandchildren are detached
                if (child.GrandChildren != null)
                {
                    foreach (var grandChild in child.GrandChildren)
                    {
                        var grandChildEntry = context.Entry(grandChild);
                        Assert.Equal(EntityState.Detached, grandChildEntry.State);
                    }
                }
            }
        }

        /// <summary>
        /// Verifies that querying the same entity multiple times doesn't cause tracking issues.
        /// </summary>
        [Fact]
        public async Task Multiple_Queries_Should_Not_Cause_Tracking_Issues()
        {
            // Arrange
            var entity = new Parent { Title = "MultipleQueries" };
            await _parentTableRepository.Save(entity);

            // Act
            var spec = new ParentSpec();
            var result1 = await _parentTableRepository.ListAsync(spec);
            var result2 = await _parentTableRepository.ListAsync(spec);
            var result3 = await _parentTableRepository.ListAsync(spec);

            // Assert
            Assert.NotEmpty(result1);
            Assert.NotEmpty(result2);
            Assert.NotEmpty(result3);
            
            // Verify no tracking issues
            var context = _parentTableRepository.GetType()
                .GetProperty("DbContext", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(_parentTableRepository) as BaseContext;
            
            Assert.NotNull(context);
            Assert.Equal(0, context.ChangeTracker.Entries().Count());
        }

        /// <summary>
        /// Verifies that pagination queries also use AsNoTracking.
        /// </summary>
        [Fact]
        public async Task Pagination_Queries_Should_Use_AsNoTracking()
        {
            // Arrange
            var entities = new List<Parent>();
            for (int i = 0; i < 10; i++)
            {
                entities.Add(new Parent { Title = $"PaginationTest{i}" });
            }
            await _parentTableRepository.SaveRange(entities);

            // Act
            var spec = new ParentSpec();
            var paginatedResult = await _parentTableRepository.PaginatedListAsync(spec, 0, 5);

            // Assert
            Assert.Equal(5, paginatedResult.Items.Count);
            
            // Verify entities are not tracked
            var context = _parentTableRepository.GetType()
                .GetProperty("DbContext", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(_parentTableRepository) as BaseContext;
            
            Assert.NotNull(context);
            
            foreach (var entity in paginatedResult.Items)
            {
                var entry = context.Entry(entity);
                Assert.Equal(EntityState.Detached, entry.State);
            }
        }

        // Custom specification for testing
        public class CustomTitleSpec : EntitySpecification<Parent>
        {
            public CustomTitleSpec(string title)
            {
                Query.Where(x => x.Title == title);
            }
        }
    }
} 