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
        Snack,
        Drink,
        Dessert,
        Seafood,
        MainDish,
        Support
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
        Normal,
        Student,
        WhiteCollar,
        Tourist,
        Foodie
    }

    public enum GamePhase
    {
        Early,
        Mid,
        Late
    }
}
