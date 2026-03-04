using PersistenceToolkit.Abstractions.Specifications;
using PersistenceToolkit.Domain;
using System.Reflection;
using System;

namespace PersistenceToolkit.Abstractions.Specifications
{
    public abstract class EntitySpecification<T> : Specification<T>
        where T : Entity
    {
        public bool IgnoreCompanyFilter { get; set; }
        public bool IncludeDeletedRecords { get; set; }
        public EntitySpecification()
        {
            Query.PostProcessingAction(a =>
            {
                var lst = (a as IEnumerable<Entity>)?.ToList() ?? new List<Entity>();
                CaptureLoadTimeSnapshot(lst);
                RemoveDeletedEntries(lst);
                return a;
            });
        }

        private void RemoveDeletedEntries(List<Entity> lst)
        {
            if (IncludeDeletedRecords) return;
            foreach (var item in lst)
            {
                RemoveDeletedEntries(item);
            }
        }

        private void RemoveDeletedEntries(Entity entity)
        {
            RemoveDeletedEntriesFromEntity(entity);
        }

        private void RemoveDeletedEntriesFromEntity(Entity entity)
        {
            if (entity == null) return;

            var props = entity.GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => p.CanRead && p.GetIndexParameters().Length == 0);

            foreach (var prop in props)
            {
                var value = prop.GetValue(entity);
                if (value == null) continue;

                switch (value)
                {
                    case Entity childEntity:
                        RemoveDeletedEntriesFromEntity(childEntity);
                        break;

                    case IList<Entity> list:
                        RemoveDeletedEntitiesFromList(list);
                        break;

                    case IEnumerable<Entity> collection:
                        RemoveDeletedEntitiesFromCollection(collection, prop, entity);
                        break;

                    case System.Collections.IEnumerable enumerable:
                        RemoveDeletedEntitiesFromEnumerable(enumerable);
                        break;
                }
            }
        }

        private void RemoveDeletedEntitiesFromList(IList<Entity> list)
        {
            // Remove from end to beginning to avoid index issues
            for (int i = list.Count - 1; i >= 0; i--)
            {
                var item = list[i];
                if (item != null)
                {
                    if (item.IsDeleted)
                    {
                        list.RemoveAt(i);
                    }
                    else
                    {
                        RemoveDeletedEntriesFromEntity(item);
                    }
                }
            }
        }

        private void RemoveDeletedEntitiesFromCollection(IEnumerable<Entity> collection, PropertyInfo prop, Entity parent)
        {
            var list = collection.ToList();
            var deletedItems = new List<Entity>();

            foreach (var item in list)
            {
                if (item != null)
                {
                    if (item.IsDeleted)
                    {
                        deletedItems.Add(item);
                    }
                    else
                    {
                        RemoveDeletedEntriesFromEntity(item);
                    }
                }
            }

            // Remove deleted items from the collection
            foreach (var deletedItem in deletedItems)
            {
                if (collection is IList<Entity> listCollection)
                {
                    listCollection.Remove(deletedItem);
                }
                else if (collection is ICollection<Entity> coll)
                {
                    coll.Remove(deletedItem);
                }
                // For other collection types, we need to create a new collection of the correct type
                else if (prop.CanWrite)
                {
                    var filteredItems = list.Where(x => !x.IsDeleted).ToList();
                    var newCollection = CreateCollectionOfCorrectType(prop.PropertyType, filteredItems);
                    prop.SetValue(parent, newCollection);
                    break; // Exit after setting the new collection
                }
            }
        }

        private object CreateCollectionOfCorrectType(Type collectionType, List<Entity> items)
        {
            // Get the element type from the collection type
            Type elementType = null;
            
            if (collectionType.IsGenericType)
            {
                var genericArgs = collectionType.GetGenericArguments();
                if (genericArgs.Length == 1)
                {
                    elementType = genericArgs[0];
                }
            }
            else if (collectionType.IsArray)
            {
                elementType = collectionType.GetElementType();
            }

            if (elementType == null)
            {
                // Fallback to List<Entity> if we can't determine the type
                return items;
            }

            // Cast items to the correct type
            var typedItems = items.Where(item => elementType.IsAssignableFrom(item.GetType())).ToList();

            // Create the appropriate collection type
            if (collectionType.IsAssignableFrom(typeof(List<>).MakeGenericType(elementType)))
            {
                var listType = typeof(List<>).MakeGenericType(elementType);
                var list = Activator.CreateInstance(listType);
                var addMethod = listType.GetMethod("Add");
                
                foreach (var item in typedItems)
                {
                    addMethod.Invoke(list, new object[] { item });
                }
                return list;
            }
            else if (collectionType.IsAssignableFrom(typeof(ICollection<>).MakeGenericType(elementType)))
            {
                var listType = typeof(List<>).MakeGenericType(elementType);
                var list = Activator.CreateInstance(listType);
                var addMethod = listType.GetMethod("Add");
                
                foreach (var item in typedItems)
                {
                    addMethod.Invoke(list, new object[] { item });
                }
                return list;
            }
            else if (collectionType.IsArray)
            {
                var array = Array.CreateInstance(elementType, typedItems.Count);
                for (int i = 0; i < typedItems.Count; i++)
                {
                    array.SetValue(typedItems[i], i);
                }
                return array;
            }
            else
            {
                // Try to create an instance of the collection type and add items
                try
                {
                    var collection = Activator.CreateInstance(collectionType);
                    var addMethod = collectionType.GetMethod("Add");
                    
                    if (addMethod != null)
                    {
                        foreach (var item in typedItems)
                        {
                            addMethod.Invoke(collection, new object[] { item });
                        }
                        return collection;
                    }
                }
                catch
                {
                    // If we can't create the collection, fall back to the original items
                }
                
                return items;
            }
        }

        private void RemoveDeletedEntitiesFromEnumerable(System.Collections.IEnumerable enumerable)
        {
            foreach (var item in enumerable)
            {
                if (item is Entity entity)
                {
                    RemoveDeletedEntriesFromEntity(entity);
                }
            }
        }

        private void CaptureLoadTimeSnapshot(List<Entity> lst)
        {
            foreach (var entity in lst)
            {
                AggregateWalker.TraverseEntities(entity, item =>
                {
                    item.CaptureLoadTimeSnapshot();
                });
            }
        }
    }
    public abstract class EntitySpecification<T, TResult> : Specification<T, TResult>
        where T : Entity
    {
        public bool IgnoreCompanyFilter { get; set; }
        public bool IncludeDeletedRecords { get; set; }
    }
}