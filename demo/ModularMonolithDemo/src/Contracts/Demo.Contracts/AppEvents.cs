namespace ModularMonolithDemo.Contracts;

/// <summary>
/// Application-level event contracts (no magic strings). Keep these centralized.
/// </summary>
public static class AppEvents
{
    public static class UI
    {
        public const string InventoryChanged = "ui.inventory.changed";
        public const string InventoryRefresh = "ui.inventory.refresh";
    }
}
