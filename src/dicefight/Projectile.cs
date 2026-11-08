namespace dicefight;
public sealed class Projectile(int owner, float x, float y, int direction, int damage, double started)
{
    public int Owner=owner, Direction=direction, Damage=damage;
    public float X=x, Y=y;
    public double Started=started;
    public bool Consumed;
    public RectangleF Box => new(X-GameBalance.ProjectileWidth/2,Y-GameBalance.ProjectileHeight/2,
        GameBalance.ProjectileWidth,GameBalance.ProjectileHeight);
}
