namespace libs.common
{
    using System;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;

    public static class Extensions
    {


        public static void ForEach<T>(this IEnumerable<T> collection, Action<T> act)
        {
            if (collection != null && act != null)
            {
                foreach (var item in collection)
                {
                    act(item);
                }
            }
        }

        public static Dictionary<char, int> CharMap = null;
        public static object lockObject = new object();

        public static string NormalizeRowKey(string id)
        {
            var result = id;
            if (CharMap == null)
            {
                lock (lockObject)
                {
                    if (CharMap == null)
                    {
                        InitializeCharMap();
                    }
                }
            }

            foreach (var entry in CharMap)
            {
                if (result.IndexOf(entry.Key) > -1)
                {
                    result = result.Replace(string.Empty + entry.Key, "[[" + entry.Value + "]]");
                }
            }

            return result;
        }


        public static T RunWithInstrumentation<T>(Func<T> method, string friendlyName, ILogger log)
        {
            var result = default(T);
            var startTime = DateTime.UtcNow;
            if (method != null)
            {
                result = method.Invoke();
            }

            var elapsed = DateTime.UtcNow - startTime;
            var message = friendlyName + " duration : " + elapsed;
            log.LogInformation(message);
            Console.WriteLine(message);
            return result;
        }

        public static void InitializeCharMap()
        {
            CharMap = new Dictionary<char, int>();
            CharMap.Add('/', (int)'/');
            CharMap.Add('\\', (int)'\\');
            CharMap.Add('#', (int)'#');
            CharMap.Add('?', (int)'?');
            CharMap.Add('\t', (int)'\t');
            CharMap.Add('\n', (int)'\n');
            CharMap.Add('\r', (int)'\r');
            CharMap.Add(':', (int)':');
            CharMap.Add('\'', (int)'\'');
            CharMap.Add('-', (int)'-');
            CharMap.Add(' ', (int)' ');
            CharMap.Add('!', (int)'!');
        }

    }
}