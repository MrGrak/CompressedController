using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Game1
{
    //Compressed Controller
    //reducing the states of a controller to a byte - MrGrak 2021
    //will map and display gamepad controller to ui on screen
    //will also reduce controller state to a byte, displayed in title

    public class Game1 : Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        string con = "controller state : ";


        

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

            Window.Title = con + Challenge1.CompressedState +
                " / buff index : " + Challenge1.InputRecordingCounter +
                " / buff size : " + Challenge1.InputRecBufferSize;
            if (Challenge1.Recording)
            { Window.Title += " - recording"; }
            else
            { Window.Title += " - writing"; }

            Window.Title += " GPS:" + Challenge1.size_GPS;
            Window.Title += " GIS:" + Challenge1.size_GIS;

        }
    }
}