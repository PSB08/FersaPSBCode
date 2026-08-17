using System.Collections.Generic;

namespace Work.PSB.Code.RunSystem
{
    public static class RunEventActionRuntimeStore
    {
        private static readonly Dictionary<string, int> IntValues = new Dictionary<string, int>();
        
        public static bool TryGetInt(string key, out int value)
        {
            value = 0;
            return !string.IsNullOrWhiteSpace(key) && IntValues.TryGetValue(key, out value);
        }
        
        public static void SetInt(string key, int value)
        {
            if (string.IsNullOrWhiteSpace(key))
                return;
            
            IntValues[key] = value;
        }
        
        public static void Remove(string key)
        {
            if (!string.IsNullOrWhiteSpace(key))
                IntValues.Remove(key);
        }
        
        public static void ClearAll()
        {
            IntValues.Clear();
        }
        
    }
}
