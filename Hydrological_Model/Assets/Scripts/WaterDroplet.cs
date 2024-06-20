public class WaterDroplet 
{
    public float posX;
    public float posY;
    public float dirX;
    public float dirY;
    public float speed;
    public float volume;
    public float sediment;

    public WaterDroplet(float posX, float posY, float dirX, float dirY, float speed, float volume) 
    {
        this.posX = posX;
        this.posY = posY;
        this.dirX = dirX;
        this.dirY = dirY;
        this.speed = speed;
        this.volume = volume;
    }
    public WaterDroplet(float posX, float posY, float dirX, float dirY, float speed, float volume, float sediment)
    {
        this.posX = posX;
        this.posY = posY;
        this.dirX = dirX;
        this.dirY = dirY;
        this.speed = speed;
        this.volume = volume;
        this.sediment = sediment;
    }
}
