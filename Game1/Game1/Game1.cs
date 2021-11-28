using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Game1
{
    //Compressed Controller
    //reducing the states of a controller to a byte - MrGrak 2021
    //will map and display gamepad controller to ui on screen
    //will also reduce controller state to a byte, displayed in title

    public enum Direction : byte
    {   //represents possible controller directions  
        None, Up, UpRight, Right, DownRight, Down, DownLeft, Left, UpLeft
    }

    public struct GameInputStruct //this struct is 24 bytes (managed)
    {
        public bool A; //this is actually 4 bytes (managed)
        public bool B;
        public bool X;
        public bool Y;
        public bool Start;
        public Direction Direction; //this is 1 byte in size, as expected

        public GameInputStruct(
            bool a, bool b, bool x, bool y,
            bool start, Direction direction)
        { A = a; B = b; X = x; Y = y; Start = start; Direction = direction; }
    }

    public class Game1 : Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        
        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }
        
        protected override void Initialize() { base.Initialize(); }
        
        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);
            Challenge1.Constructor(graphics, spriteBatch, this);
        }
        
        protected override void UnloadContent() { }
        
        protected override void Update(GameTime gameTime)
        {
            Challenge1.Update();
        }
        
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(new Color(40, 40, 40));
            Challenge1.Draw();
        }
    }
}