namespace PersistenceToolkit.Persistence.Persistence
{
    internal static class NavigationIgnoreTracker
    {
        internal static readonly Dictionary<Type, List<string>> IgnoredNavigationsOnUpdate = new();

        internal static void MarkIgnored<T>(string propertyName)
        {
            var type = typeof(T);
            if (!IgnoredNavigationsOnUpdate.ContainsKey(type))
                IgnoredNavigationsOnUpdate[type] = new List<string>();

            IgnoredNavigationsOnUpdate[type].Add(propertyName);
        }

        //internal static Dictionary<Type, List<string>> CollectAndReset()
        //{
        //    var copy = new Dictionary<Type, List<string>>(IgnoredNavigationsOnUpdate);
        //    IgnoredNavigationsOnUpdate.Clear();
        //    return copy;
        //}
    }
}
