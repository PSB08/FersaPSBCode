namespace PSB.Code.CoreSystem.SaveSystem
{
    public interface ISaveStore
    {
        bool TryGetRawDataById(int saveId, out string rawJson);
        void DeleteById(SaveId id);
    }
}