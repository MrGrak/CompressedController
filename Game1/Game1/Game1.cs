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
            Input.Constructor(graphics, spriteBatch);
        }
        
        protected override void UnloadContent() { }
        
        protected override void Update(GameTime gameTime)
        {
            Input.Update();
        }
        
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(new Color(40, 40, 40));
            Input.Draw();

            Window.Title = con + Input.CompressedState +
                " / buff index : " + Input.InputRecordingCounter +
                " / buff size : " + Input.InputRecBufferSize;
            if (Input.Recording)
            { Window.Title += " - recording"; }
            else
            { Window.Title += " - playing back"; }
        }
    }
}