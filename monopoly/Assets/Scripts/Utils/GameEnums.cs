namespace Monopoly.Utils
{
    public enum TileType
    {
        None,
        Shop,
        Upgrade,
        Event,
        Start,
        Empty
    }

    public enum ShopCategory
    {
        Exotic,
        Snack,
        Chinese,
        FastFood
    }

    public enum ShopRole
    {
        Income,
        Control,
        Support,
        Core
    }

    public enum ShopBranchType
    {
        Default,
        Income,
        Control,
        Support,
        Special
    }

    public enum CustomerType
    {
        Student = 0,
        Merchant = 1,
        Worker = 2
    }

    public enum GamePhase
    {
        Early,
        Mid,
        Late
    }
}
