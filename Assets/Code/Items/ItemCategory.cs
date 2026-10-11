namespace ChefGame.Items
{
    /// <summary>Broad gameplay category used as an item-data equivalent of a tag.</summary>
    public enum ItemCategory
    {
        Ingredient,
        Utensil
    }

    /// <summary>Stable gameplay identity for a specific item definition.</summary>
    public enum ItemType
    {
        None = 0,
        Tomato = 1,
        TomatoHalf = 2,
        Knife = 3,
        Onion = 4,
        PeeledOnion = 5,
        Peeler = 6,
        PeeledOnionHalf = 7,
        Carrot = 8,
        PeeledCarrot = 9,
        CutCarrot = 10
    }

    public enum IngredientType
    {
        None = 0,
        Tomato = 1,
        HalfTomato = 2,
        QuarterTomato = 3,
        PeeledTomato = 4,
        Onion = 5,
        HalfOnion = 6,
        QuarterOnion = 7,
        PeeledOnion = 8,
        Carrot = 9,
        HalfCarrot = 10,
        QuarterCarrot = 11,
        PeeledCarrot = 12,
    }
    
}
