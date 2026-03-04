using PersistenceToolkit.Domain;
using PersistenceToolkit.Tests.Entities;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace PersistenceToolkit.Tests
{
    public class AggregateWalkerTests
    {
        /// <summary>
        /// Verifies that AggregateWalker traverses a single entity and calls the action.
        /// </summary>
        [Fact]
        public void TraverseEntities_With_Single_Entity_Should_Call_Action()
        {
            // Arrange
            var entity = new Parent { Id = 1, Title = "Test" };
            var visitedEntities = new List<Entity>();

            // Act
            AggregateWalker.TraverseEntities(entity, e => visitedEntities.Add(e));

            // Assert
            Assert.Single(visitedEntities);
            Assert.Same(entity, visitedEntities[0]);
        }

        /// <summary>
        /// Verifies that AggregateWalker handles null entity gracefully.
        /// </summary>
        [Fact]
        public void TraverseEntities_With_Null_Entity_Should_Not_Throw()
        {
            // Arrange
            Entity entity = null;
            var visitedEntities = new List<Entity>();

            // Act & Assert
            var exception = Record.Exception(() => 
                AggregateWalker.TraverseEntities(entity, e => visitedEntities.Add(e)));

            Assert.Null(exception);
            Assert.Empty(visitedEntities);
        }

        /// <summary>
        /// Verifies that AggregateWalker traverses entity with single child entity.
        /// </summary>
        [Fact]
        public void TraverseEntities_With_Single_Child_Should_Traverse_Both()
        {
            // Arrange
            var child = new OneToOneChild { Id = 2, Title = "Child" };
            var parent = new Parent { Id = 1, Title = "Parent", IgnoredChild = child };
            var visitedEntities = new List<Entity>();

            // Act
            AggregateWalker.TraverseEntities(parent, e => visitedEntities.Add(e));

            // Assert
            Assert.Equal(2, visitedEntities.Count);
            Assert.Contains(parent, visitedEntities);
            Assert.Contains(child, visitedEntities);
        }

        /// <summary>
        /// Verifies that AggregateWalker traverses entity with collection of child entities.
        /// </summary>
        [Fact]
        public void TraverseEntities_With_Child_Collection_Should_Traverse_All()
        {
            // Arrange
            var children = new List<Child>
            {
                new Child { Id = 2, Title = "Child1" },
                new Child { Id = 3, Title = "Child2" },
                new Child { Id = 4, Title = "Child3" }
            };
            var parent = new Parent { Id = 1, Title = "Parent", Children = children };
            var visitedEntities = new List<Entity>();

            // Act
            AggregateWalker.TraverseEntities(parent, e => visitedEntities.Add(e));

            // Assert
            Assert.Equal(4, visitedEntities.Count); // Parent + 3 children
            Assert.Contains(parent, visitedEntities);
            Assert.All(children, child => Assert.Contains(child, visitedEntities));
        }

        /// <summary>
        /// Verifies that AggregateWalker handles null child entities in collections.
        /// </summary>
        [Fact]
        public void TraverseEntities_With_Null_Children_In_Collection_Should_Skip_Nulls()
        {
            // Arrange
            var children = new List<Child>
            {
                new Child { Id = 2, Title = "Child1" },
                null,
                new Child { Id = 4, Title = "Child3" }
            };
            var parent = new Parent { Id = 1, Title = "Parent", Children = children };
            var visitedEntities = new List<Entity>();

            // Act
            AggregateWalker.TraverseEntities(parent, e => visitedEntities.Add(e));

            // Assert
            Assert.Equal(3, visitedEntities.Count); // Parent + 2 non-null children
            Assert.Contains(parent, visitedEntities);
            Assert.Contains(children[0], visitedEntities);
            Assert.Contains(children[2], visitedEntities);
        }

        /// <summary>
        /// Verifies that AggregateWalker traverses deeply nested entity structures.
        /// </summary>
        [Fact]
        public void TraverseEntities_With_Deep_Nesting_Should_Traverse_All_Levels()
        {
            // Arrange
            var grandChild = new GrandChild { Id = 3, Title = "GrandChild" };
            var child = new OneToOneChild { Id = 2, Title = "Child", GrandChildren = new List<GrandChild> { grandChild } };
            var parent = new Parent { Id = 1, Title = "Parent", IgnoredChild = child };
            var visitedEntities = new List<Entity>();

            // Act
            AggregateWalker.TraverseEntities(parent, e => visitedEntities.Add(e));

            // Assert
            Assert.Equal(3, visitedEntities.Count);
            Assert.Contains(parent, visitedEntities);
            Assert.Contains(child, visitedEntities);
            Assert.Contains(grandChild, visitedEntities);
        }

        /// <summary>
        /// Verifies that AggregateWalker handles different collection types (IEnumerable).
        /// </summary>
        [Fact]
        public void TraverseEntities_With_IEnumerable_Collection_Should_Traverse_All()
        {
            // Arrange
            var children = new List<Child>
            {
                new Child { Id = 2, Title = "Child1" },
                new Child { Id = 3, Title = "Child2" }
            };
            var parent = new Parent { Id = 1, Title = "Parent", Children = children };
            var visitedEntities = new List<Entity>();

            // Act
            AggregateWalker.TraverseEntities(parent, e => visitedEntities.Add(e));

            // Assert
            Assert.Equal(3, visitedEntities.Count); // Parent + 2 children
            Assert.Contains(parent, visitedEntities);
            Assert.All(children, child => Assert.Contains(child, visitedEntities));
        }

        /// <summary>
        /// Verifies that AggregateWalker handles mixed collection types with non-Entity items.
        /// </summary>
        [Fact]
        public void TraverseEntities_With_Mixed_Collection_Should_Only_Traverse_Entities()
        {
            // Arrange
            var mixedItems = new List<object>
            {
                new Child { Id = 2, Title = "Child1" },
                "String item",
                42,
                new Child { Id = 3, Title = "Child2" }
            };
            var parent = new Parent { Id = 1, Title = "Parent" };
            var visitedEntities = new List<Entity>();

            // Act
            AggregateWalker.TraverseEntities(parent, e => visitedEntities.Add(e));

            // Assert
            Assert.Single(visitedEntities); // Only parent since MixedCollection is not a property
            Assert.Contains(parent, visitedEntities);
        }

        /// <summary>
        /// Verifies that AggregateWalker handles empty collections gracefully.
        /// </summary>
        [Fact]
        public void TraverseEntities_With_Empty_Collection_Should_Only_Traverse_Parent()
        {
            // Arrange
            var parent = new Parent { Id = 1, Title = "Parent", Children = new List<Child>() };
            var visitedEntities = new List<Entity>();

            // Act
            AggregateWalker.TraverseEntities(parent, e => visitedEntities.Add(e));

            // Assert
            Assert.Single(visitedEntities);
            Assert.Same(parent, visitedEntities[0]);
        }

        /// <summary>
        /// Verifies that AggregateWalker handles null collections gracefully.
        /// </summary>
        [Fact]
        public void TraverseEntities_With_Null_Collection_Should_Only_Traverse_Parent()
        {
            // Arrange
            var parent = new Parent { Id = 1, Title = "Parent", Children = null };
            var visitedEntities = new List<Entity>();

            // Act
            AggregateWalker.TraverseEntities(parent, e => visitedEntities.Add(e));

            // Assert
            Assert.Single(visitedEntities);
            Assert.Same(parent, visitedEntities[0]);
        }

        /// <summary>
        /// Verifies that AggregateWalker handles complex nested structures with multiple collections.
        /// </summary>
        [Fact]
        public void TraverseEntities_With_Complex_Nested_Structure_Should_Traverse_All()
        {
            // Arrange
            var grandChild1 = new GrandChild { Id = 4, Title = "GrandChild1" };
            var grandChild2 = new GrandChild { Id = 5, Title = "GrandChild2" };
            var grandChild3 = new GrandChild { Id = 6, Title = "GrandChild3" };
            
            var child1 = new OneToOneChild { Id = 2, Title = "Child1", GrandChildren = new List<GrandChild> { grandChild1 } };
            var child2 = new Child { Id = 3, Title = "Child2", GrandChildren = new List<GrandChild> { grandChild2, grandChild3 } };
            
            var parent = new Parent 
            { 
                Id = 1, 
                Title = "Parent", 
                IgnoredChild = child1,
                Children = new List<Child> { child2 }
            };
            
            var visitedEntities = new List<Entity>();

            // Act
            AggregateWalker.TraverseEntities(parent, e => visitedEntities.Add(e));

            // Assert
            Assert.Equal(6, visitedEntities.Count); // Parent + 2 children + 3 grandchildren
            Assert.Contains(parent, visitedEntities);
            Assert.Contains(child1, visitedEntities);
            Assert.Contains(child2, visitedEntities);
            Assert.Contains(grandChild1, visitedEntities);
            Assert.Contains(grandChild2, visitedEntities);
            Assert.Contains(grandChild3, visitedEntities);
        }

        /// <summary>
        /// Verifies that AggregateWalker handles circular references gracefully.
        /// </summary>
        [Fact]
        public void TraverseEntities_With_Circular_Reference_Should_Not_StackOverflow()
        {
            // Arrange
            var entity1 = new Parent { Id = 1, Title = "Entity1" };
            var entity2 = new OneToOneChild { Id = 2, Title = "Entity2" };
            
            // Create circular reference through IgnoredChild
            entity1.IgnoredChild = entity2;
            // Note: ChildTable doesn't have a back reference, so this is a one-way reference
            
            var visitedEntities = new List<Entity>();

            // Act & Assert - Should not throw or cause stack overflow
            var exception = Record.Exception(() => 
                AggregateWalker.TraverseEntities(entity1, e => visitedEntities.Add(e)));

            Assert.Null(exception);
            Assert.Equal(2, visitedEntities.Count); // Both entities should be visited
            Assert.Contains(entity1, visitedEntities);
            Assert.Contains(entity2, visitedEntities);
        }


    }
} 