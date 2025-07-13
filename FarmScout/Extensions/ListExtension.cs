using System.Collections.ObjectModel;

namespace FarmScout.Extensions
{
    internal static class ListExtension
    {
        public static void PopulateFrom<T>(this Collection<T> destination, IEnumerable<T> source)
        {
            destination.Clear();
            foreach (var item in source)
            {
                destination.Add(item);
            }
        }

        public static async Task PopulateFromAsync<T>(this Collection<T> destination, Func<Task<IEnumerable<T>>> source)
        {
            destination.Clear();
            var items = await source();
            foreach (var item in items)
            {
                destination.Add(item);
            }
        }
        public static async Task PopulateFromAsync<T>(this Collection<T> destination, Func<Task<IEnumerable<T>>> source, Func<IEnumerable<T>, IEnumerable<T>> linq)
        {
            destination.Clear();
            var items = await source();            
            foreach (var item in linq(items))
            {
                destination.Add(item);
            }
        }
    }
}
