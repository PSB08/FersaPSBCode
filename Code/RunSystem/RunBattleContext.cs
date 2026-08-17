namespace Work.PSB.Code.RunSystem
{
    public static class RunBattleContext
    {
        public static bool IsActive { get; private set; }
        public static RunNodeType NodeType { get; private set; }
        
        public static void Set(RunNodeType nodeType)
        {
            IsActive = true;
            NodeType = nodeType;
        }
        
        public static void Clear()
        {
            IsActive = false;
            NodeType = default;
        }
        
    }
}
