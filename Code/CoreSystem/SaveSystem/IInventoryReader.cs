namespace PSB.Code.CoreSystem.SaveSystem
{
    public interface IInventoryReader
    {
        bool TryGetInventorySnapshot(int inventorySaveId, out InvenCollection snapshot);
        (int itemId, int amount)[] GetInventoryAllSlots(int inventorySaveId);
    }
}