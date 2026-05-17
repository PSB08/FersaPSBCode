namespace PSB.Code.CoreSystem.SaveSystem
{
    public interface ISaveable
    {
        SaveId SaveId { get; }
        string GetSaveData();
        void RestoreSaveData(string saveData);
    }
}