using Microsoft.Extensions.DependencyInjection;
using PersistenceToolkit.Abstractions.Repositories;
using PersistenceToolkit.Abstractions.Specifications;
using PersistenceToolkit.Domain;
using PersistenceToolkit.Tests.Initializers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using System;
using PersistenceToolkit.Tests.Entities;

namespace PersistenceToolkit.Tests
{
    public class LoadTimeSnapshotTests : IDisposable
    {
        private readonly IAggregateRepository<Parent> _parentTableRepository;
        private readonly ServiceProvider _serviceProvider;

        public LoadTimeSnapshotTests()
        {
            _serviceProvider = DependencyInjectionSetup.InitializeServiceProvider();
            _parentTableRepository = _serviceProvider.GetService<IAggregateRepository<Parent>>();
        }

        public void Dispose()
        {
            _serviceProvider?.Dispose();
        }



        /// <summary>
        /// Verifies that CaptureLoadTimeSnapshot sets the snapshot and HasChange returns false.
        /// </summary>
        [Fact]
        public async Task CaptureLoadTimeSnapshot_Should_Set_Snapshot_And_Reset_Changes()
        {
            // Arrange
            var entity = new Parent { Title = "SnapshotTest" };

            // Act
            entity.CaptureLoadTimeSnapshot();

            // Assert
            Assert.False(entity.HasChange());
        }

        /// <summary>
        /// Verifies that modifying an entity after capturing snapshot detects changes.
        /// </summary>
        [Fact]
        public async Task Modifying_Entity_After_Snapshot_Should_Detect_Changes()
        {
            // Arrange
            var entity = new Parent { Title = "OriginalTitle" };
            entity.CaptureLoadTimeSnapshot();

            // Act
            entity.Title = "ModifiedTitle";

            // Assert
            Assert.True(entity.HasChange());
        }

        /// <summary>
        /// Verifies that saving an entity automatically captures the snapshot.
        /// </summary>
        [Fact]
        public async Task Save_Should_Automatically_Capture_Snapshot()
        {
            // Arrange
            var entity = new Parent { Title = "AutoSnapshot" };

            // Act
            await _parentTableRepository.Save(entity);

            // Assert
            Assert.False(entity.HasChange());
        }

        /// <summary>
        /// Verifies that loading an entity from database captures the snapshot.
        /// </summary>
        [Fact]
        public async Task Load_From_Database_Should_Capture_Snapshot()
        {
            // Arrange
            var entity = new Parent { Title = "LoadSnapshot" };
            await _parentTableRepository.Save(entity);

            // Act
            var spec = new ParentSpec();
            var loadedEntity = await _parentTableRepository.FirstOrDefaultAsync(spec);

            // Assert
            Assert.NotNull(loadedEntity);
            Assert.False(loadedEntity.HasChange());
        }

        /// <summary>
        /// Verifies that modifying a loaded entity detects changes.
        /// </summary>
        [Fact]
        public async Task Modifying_Loaded_Entity_Should_Detect_Changes()
        {
            // Arrange
            var entity = new Parent { Title = "LoadAndModify" };
            await _parentTableRepository.Save(entity);

            var spec = new ParentSpec();
            var loadedEntity = await _parentTableRepository.FirstOrDefaultAsync(spec);

            // Act
            loadedEntity.Title = "ModifiedAfterLoad";

            // Assert
            Assert.True(loadedEntity.HasChange());
        }

        /// <summary>
        /// Verifies that saving a modified entity resets the change detection.
        /// </summary>
        [Fact]
        public async Task Save_Modified_Entity_Should_Reset_Change_Detection()
        {
            // Arrange
            var entity = new Parent { Title = "OriginalTitle" };
            await _parentTableRepository.Save(entity);

            var spec = new ParentSpec();
            var loadedEntity = await _parentTableRepository.FirstOrDefaultAsync(spec);
            loadedEntity.Title = "ModifiedTitle";

            // Act
            await _parentTableRepository.Save(loadedEntity);

            // Assert
            Assert.False(loadedEntity.HasChange());
        }

        /// <summary>
        /// Verifies that multiple property changes are detected.
        /// </summary>
        [Fact]
        public async Task Multiple_Property_Changes_Should_Be_Detected()
        {
            // Arrange
            var entity = new Parent { Title = "MultipleChanges" };
            entity.CaptureLoadTimeSnapshot();

            // Act
            entity.Title = "ChangedTitle";
            entity.Children = new List<Child> { new Child { Title = "Child" } };

            // Assert
            Assert.True(entity.HasChange());
        }

        /// <summary>
        /// Verifies that nested entity changes are detected.
        /// </summary>
        [Fact]
        public async Task Nested_Entity_Changes_Should_Be_Detected()
        {
            // Arrange
            var parent = new Parent { Title = "Parent" };
            var child = new Child { Title = "Child" };
            parent.Children = new List<Child> { child };

            await _parentTableRepository.Save(parent);

            var spec = new ParentSpec();
            var loadedParent = await _parentTableRepository.FirstOrDefaultAsync(spec);

            // Act
            loadedParent.Children.First().Title = "ModifiedChild";

            // Assert
            Assert.True(loadedParent.HasChange());
        }

        /// <summary>
        /// Verifies that deeply nested grandchild changes are detected.
        /// </summary>
        [Fact]
        public async Task Deeply_Nested_Grandchild_Changes_Should_Be_Detected()
        {
            // Arrange
            var parent = new Parent { Title = "Parent" };
            var child = new Child { Title = "Child" };
            var grandChild = new GrandChild { Title = "GrandChild" };
            
            child.GrandChildren = new List<GrandChild> { grandChild };
            parent.Children = new List<Child> { child };

            await _parentTableRepository.Save(parent);

            var spec = new ParentSpec();
            var loadedParent = await _parentTableRepository.FirstOrDefaultAsync(spec);

            // Act
            loadedParent.Children.First().GrandChildren.First().Title = "ModifiedGrandChild";

            // Assert
            Assert.True(loadedParent.HasChange());
        }

        /// <summary>
        /// Verifies that snapshot captures the exact state at the time of capture.
        /// </summary>
        [Fact]
        public async Task Snapshot_Should_Capture_Exact_State()
        {
            // Arrange
            var entity = new Parent { Title = "ExactState" };
            entity.CaptureLoadTimeSnapshot();

            // Act
            entity.Title = "ModifiedState";
            var hasChangeAfterModify = entity.HasChange();
            
            entity.Title = "ExactState"; // Revert to original state

            // Assert
            Assert.True(hasChangeAfterModify);
            Assert.False(entity.HasChange()); // Should be false after reverting to original state
        }

        /// <summary>
        /// Verifies that snapshot works with complex objects (navigation properties).
        /// </summary>
        [Fact]
        public async Task Snapshot_Should_Work_With_Complex_Objects()
        {
            // Arrange
            var parent = new Parent
            {
                Title = "ComplexParent",
                Children = new List<Child>
                {
                    new Child { Title = "Child1" },
                    new Child { Title = "Child2" }
                }
            };

            // Act
            parent.CaptureLoadTimeSnapshot();
            var hasChangeInitially = parent.HasChange();

            parent.Children.Add(new Child { Title = "Child3" });

            // Assert
            Assert.False(hasChangeInitially);
            Assert.True(parent.HasChange());
        }

        /// <summary>
        /// Verifies that snapshot is JSON-based and handles special characters.
        /// </summary>
        [Fact]
        public async Task Snapshot_Should_Handle_Special_Characters()
        {
            // Arrange
            var entity = new Parent { Title = "Special\"Chars\n\t\r" };
            entity.CaptureLoadTimeSnapshot();

            // Act
            entity.Title = "Different\"Chars\n\t\r";
            var hasChange = entity.HasChange();

            // Assert
            Assert.True(hasChange);
        }

        /// <summary>
        /// Verifies that snapshot works with null values.
        /// </summary>
        [Fact]
        public async Task Snapshot_Should_Work_With_Null_Values()
        {
            // Arrange
            var entity = new Parent { Title = null };
            entity.CaptureLoadTimeSnapshot();

            // Act
            entity.Title = "NotNull";
            var hasChange = entity.HasChange();

            // Assert
            Assert.True(hasChange);
        }

        /// <summary>
        /// Verifies that snapshot works with empty collections.
        /// </summary>
        [Fact]
        public async Task Snapshot_Should_Work_With_Empty_Collections()
        {
            // Arrange
            var entity = new Parent
            {
                Title = "EmptyCollection",
                Children = new List<Child>()
            };
            entity.CaptureLoadTimeSnapshot();

            // Act
            entity.Children.Add(new Child { Title = "NewChild" });
            var hasChange = entity.HasChange();

            // Assert
            Assert.True(hasChange);
        }

        /// <summary>
        /// Verifies that snapshot is captured for all entities in a collection.
        /// </summary>
        [Fact]
        public async Task Snapshot_Should_Be_Captured_For_All_Entities_In_Collection()
        {
            // Arrange
            var entities = new List<Parent>
            {
                new Parent { Title = "Entity1" },
                new Parent { Title = "Entity2" },
                new Parent { Title = "Entity3" }
            };

            // Act
            await _parentTableRepository.SaveRange(entities);

            // Assert
            foreach (var entity in entities)
            {
                Assert.False(entity.HasChange());
            }
        }

        /// <summary>
        /// Verifies that snapshot is captured during post-processing in specifications.
        /// </summary>
        [Fact]
        public async Task Specification_PostProcessing_Should_Capture_Snapshot()
        {
            // Arrange
            var entity = new Parent { Title = "PostProcessing" };
            await _parentTableRepository.Save(entity);

            // Act
            var spec = new ParentSpec();
            var result = await _parentTableRepository.ListAsync(spec);
            var loadedEntity = result.FirstOrDefault();

            // Assert
            Assert.NotNull(loadedEntity);
            Assert.False(loadedEntity.HasChange());
        }

        /// <summary>
        /// Verifies that snapshot comparison is case-sensitive.
        /// </summary>
        [Fact]
        public async Task Snapshot_Comparison_Should_Be_Case_Sensitive()
        {
            // Arrange
            var entity = new Parent { Title = "CaseSensitive" };
            entity.CaptureLoadTimeSnapshot();

            // Act
            entity.Title = "casesensitive"; // Different case
            var hasChange = entity.HasChange();

            // Assert
            Assert.True(hasChange);
        }



        // Custom specification for testing
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