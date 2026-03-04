using System.Reflection;

namespace PersistenceToolkit.Domain
{
    public class AggregateWalker
    {
        public static void TraverseEntities(Entity entity, Action<Entity> action)
        {
            if (entity == null)
                return;

            action(entity);

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
                        TraverseEntities(childEntity, action);
                        break;

                    case IEnumerable<Entity> collection:
                        foreach (var item in collection)
                        {
                            if (item != null)
                                TraverseEntities(item, action);
                        }
                        break;

                    case System.Collections.IEnumerable enumerable:
                        foreach (var item in enumerable)
                        {
                            if (item is Entity nested)
                                TraverseEntities(nested, action);
                        }
                        break;
                }
            }
        }
    }
}
