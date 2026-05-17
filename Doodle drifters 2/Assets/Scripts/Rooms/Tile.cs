public enum TileType
{
    Floor,
    Wall,
    Enemy,
    Chest
}

public class Tile
{
    public int x;
    public int y;
    public TileType type;

    public Tile(int x, int y, TileType type)
    {
        this.x = x;
        this.y = y;
        this.type = type;
    }
}