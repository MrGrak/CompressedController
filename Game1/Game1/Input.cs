using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game1
{

    //todo: fix input so that player can't press more than 2 buttons
    //dont record or display more than 2 button presses either, discard additional
    //we could also split the controller into 2 bytes, on for the direction,
    //and one for the buttons, which would allow us to support more, while
    //keeping recording size very small




    public enum Direction : byte
    {   //represents possible controller directions  
        None,
        Up, UpRight, Right, DownRight, Down, DownLeft, Left, UpLeft
    }

    public struct GameInputStruct
    {
        public bool A;
        public bool B;
        public bool X;
        public bool Y;
        public bool Start;
        public Direction Direction;

        public GameInputStruct(
            bool a, bool b, bool x, bool y,
            bool start, Direction direction)
        { A = a; B = b; X = x; Y = y; Start = start; Direction = direction; }
    }

    public static class Input
    {
        public static GraphicsDeviceManager GDM;
        public static SpriteBatch SB;

        //4 recs for direction pad, 4 recs for abxy, 1 for start
        public static byte Size = 9;
        public static Texture2D Texture;
        public static List<Rectangle> Rectangles;

        //structs to store this frame and prev frames input
        public static GameInputStruct thisFrame;
        public static GameInputStruct prevFrame;

        public static GamePadState ControllerState;
        //the amount of joystick movement classified as noise
        public static float deadzone = 0.20f;

        //stores gamepad state in a byte
        public static byte CompressedState;
        //allocate a buffer of game input structs to write to - 60 fps x N seconds
        public static short InputRecBufferSize = 60 * 10;
        public static byte[] InputRecordingBuffer = new byte[InputRecBufferSize];
        public static short InputRecordingCounter = 0;

        //controls if class is recording or playing back input
        public static bool Recording = true;



        

        public static void Constructor(GraphicsDeviceManager gdm, SpriteBatch sb)
        {
            //store references to what this class needs to function
            GDM = gdm; SB = sb;
            //create a tiny texture to draw with + tint
            Texture = new Texture2D(GDM.GraphicsDevice, 1, 1);
            Texture.SetData<Color>(new Color[] { Color.White });
            CompressedState = 0; //reset controller state

            //place rectangles in controller positions
            Point Pos = new Point(50, 50); //controls overall ui position
            byte scale = 8;
            int Off = scale * 7; //padding between dir buttons and abxy buttons
            Rectangles = new List<Rectangle>();
            for (int i = 0; i < Size; i++)
            {
                switch (i)
                {
                    case 0: { Rectangles.Add(new Rectangle(Pos.X + 0, Pos.Y + 0, scale, scale)); break; } //direction pad up
                    case 1: { Rectangles.Add(new Rectangle(Pos.X + scale, Pos.Y + scale, scale, scale)); break; } //right
                    case 2: { Rectangles.Add(new Rectangle(Pos.X + 0, Pos.Y + scale * 2, scale, scale)); break; } //down
                    case 3: { Rectangles.Add(new Rectangle(Pos.X - scale, Pos.Y + scale, scale, scale)); break; } //left
                    case 4: { Rectangles.Add(new Rectangle(Pos.X + Off + 0, Pos.Y + 0, scale, scale)); break; } //A
                    case 5: { Rectangles.Add(new Rectangle(Pos.X + Off + scale, Pos.Y + scale, scale, scale)); break; } //B
                    case 6: { Rectangles.Add(new Rectangle(Pos.X + Off + 0, Pos.Y + scale * 2, scale, scale)); break; } //X
                    case 7: { Rectangles.Add(new Rectangle(Pos.X + Off - scale, Pos.Y + scale, scale, scale)); break; } //Y
                    case 8: { Rectangles.Add(new Rectangle(Pos.X + 24, Pos.Y + scale, scale * 2, scale)); break; } //start button

                    default: { break; }
                }
            }
        }

        public static void Update()
        {
            if(Recording)
            {
                //map controller to input struct, store in input buffer

                //store this frames input in prev frame
                prevFrame = thisFrame;
                //clear this frame's input
                thisFrame = new GameInputStruct();
                //assume player one, this is brittle
                ControllerState = GamePad.GetState(PlayerIndex.One);
                //map controller state to struct - useful when mapping diff inputs (like keys)
                MapController(ref ControllerState, ref thisFrame);
                //convert input struct to byte, store in buffer
                InputRecordingBuffer[InputRecordingCounter] = ConvertToByte(thisFrame);

                //update compressed state that we draw later
                CompressedState = InputRecordingBuffer[InputRecordingCounter];

                //using a counter, track where input is stored in buffer
                InputRecordingCounter++;
                //treat this data stream as a ring, loop back to start
                if (InputRecordingCounter >= InputRecBufferSize)
                {
                    InputRecordingCounter = 0;
                    Recording = false; //begin playback of buffer
                }
            }
            else
            {
                //play back input from input buffer

                //store this frames input in prev frame
                prevFrame = thisFrame;
                //update this frame's input
                thisFrame = ConvertToInputStruct(InputRecordingBuffer[InputRecordingCounter]);

                //update compressed state that we draw later
                CompressedState = InputRecordingBuffer[InputRecordingCounter];

                //using a counter, track where input is stored in buffer
                InputRecordingCounter++;
                //treat this data stream as a ring, loop back to start
                if (InputRecordingCounter >= InputRecBufferSize)
                {
                    InputRecordingCounter = 0;
                    Recording = true; //begin recording into buffer
                }
            }
        }

        public static void Draw()
        {
            SB.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
            
            //draw all buttons with black background
            for (int i = 0; i < Size; i++)
            {
                if (Recording) //visually show player we are recording
                { DrawRectangle(Rectangles[i], Color.DarkRed); }
                else { DrawRectangle(Rectangles[i], Color.DarkGreen); }
            }

            //draw direction buttons being used in white (up/down)
            if (thisFrame.Direction == Direction.Up
                || thisFrame.Direction == Direction.UpLeft
                || thisFrame.Direction == Direction.UpRight)
            { DrawRectangle(Rectangles[0], Color.White); }
            else if (thisFrame.Direction == Direction.Down
                || thisFrame.Direction == Direction.DownLeft
                || thisFrame.Direction == Direction.DownRight)
            { DrawRectangle(Rectangles[2], Color.White); }
            //draw buttons being used in white (left/right)
            if (thisFrame.Direction == Direction.Right
                || thisFrame.Direction == Direction.UpRight
                || thisFrame.Direction == Direction.DownRight)
            { DrawRectangle(Rectangles[1], Color.White); }
            else if (thisFrame.Direction == Direction.Left
                || thisFrame.Direction == Direction.DownLeft
                || thisFrame.Direction == Direction.UpLeft)
            { DrawRectangle(Rectangles[3], Color.White); }

            //draw buttons pressed down in white (abxy + start)
            if (thisFrame.A) { DrawRectangle(Rectangles[6], Color.White); }
            if (thisFrame.B) { DrawRectangle(Rectangles[5], Color.White); }
            if (thisFrame.X) { DrawRectangle(Rectangles[7], Color.White); }
            if (thisFrame.Y) { DrawRectangle(Rectangles[4], Color.White); }
            if (thisFrame.Start) { DrawRectangle(Rectangles[8], Color.White); }

            SB.End();
        }

        public static void DrawRectangle(Rectangle Rec, Color color)
        {
            Vector2 pos = new Vector2(Rec.X, Rec.Y);
            SB.Draw(Texture, pos, Rec,
                color * 1.0f,
                0, Vector2.Zero, 1.0f,
                SpriteEffects.None, 0.00001f);
        }


        

        //methods to map gamepad state to game input struct (add more, like for keyboard, etc)

        public static void MapController(ref GamePadState GPS, ref GameInputStruct GIS)
        {

            #region Map gamepad left joystick

            if (GPS.ThumbSticks.Left.X > deadzone &
                GPS.ThumbSticks.Left.Y > deadzone)
            {
                GIS.Direction = Direction.UpRight;
            }
            else if (GPS.ThumbSticks.Left.X < -deadzone &
                GPS.ThumbSticks.Left.Y > deadzone)
            {
                GIS.Direction = Direction.UpLeft;
            }

            else if (GPS.ThumbSticks.Left.X > deadzone &
                GPS.ThumbSticks.Left.Y < -deadzone)
            {
                GIS.Direction = Direction.DownRight;
            }
            else if (GPS.ThumbSticks.Left.X < -deadzone &
                GPS.ThumbSticks.Left.Y < -deadzone)
            {
                GIS.Direction = Direction.DownLeft;
            }

            else if (GPS.ThumbSticks.Left.X > deadzone)
            {
                GIS.Direction = Direction.Right;
            }
            else if (GPS.ThumbSticks.Left.X < -deadzone)
            {
                GIS.Direction = Direction.Left;
            }
            else if (GPS.ThumbSticks.Left.Y > deadzone)
            {
                GIS.Direction = Direction.Up;
            }
            else if (GPS.ThumbSticks.Left.Y < -deadzone)
            {
                GIS.Direction = Direction.Down;
            }

            #endregion

            #region Map gamepad Dpad

            if (GPS.IsButtonDown(Buttons.DPadRight) &
                GPS.IsButtonDown(Buttons.DPadUp))
            { GIS.Direction = Direction.UpRight; }
            else if (GPS.IsButtonDown(Buttons.DPadLeft) &
                GPS.IsButtonDown(Buttons.DPadUp))
            { GIS.Direction = Direction.UpLeft;}
            else if (GPS.IsButtonDown(Buttons.DPadLeft) &
                GPS.IsButtonDown(Buttons.DPadDown))
            { GIS.Direction = Direction.DownLeft; }
            else if (GPS.IsButtonDown(Buttons.DPadRight) &
                GPS.IsButtonDown(Buttons.DPadDown))
            { GIS.Direction = Direction.DownRight; }
            else if (GPS.IsButtonDown(Buttons.DPadRight))
            { GIS.Direction = Direction.Right; }
            else if (GPS.IsButtonDown(Buttons.DPadLeft))
            { GIS.Direction = Direction.Left; }
            else if (GPS.IsButtonDown(Buttons.DPadUp))
            { GIS.Direction = Direction.Up; }
            else if (GPS.IsButtonDown(Buttons.DPadDown))
            { GIS.Direction = Direction.Down; }

            #endregion

            #region Map Start Button

            //map singular start button 
            if (GPS.IsButtonDown(Buttons.Start))
            {
                GIS.Start = true;
            }

            #endregion

            #region Map Default Button Inputs

            if (GPS.IsButtonDown(Buttons.A))
            {
                GIS.A = true;
            }
            if (GPS.IsButtonDown(Buttons.X))
            {
                GIS.X = true;
            }

            if (GPS.IsButtonDown(Buttons.B))
            {
                GIS.B = true;
            }
            if (GPS.IsButtonDown(Buttons.Y))
            {
                GIS.Y = true;
            }

            #endregion

            #region Map Additional Button Inputs
            
            if (GPS.IsButtonDown(Buttons.LeftTrigger))
            {
                GIS.X = true;
            }
            if (GPS.IsButtonDown(Buttons.RightTrigger))
            {
                GIS.Y = true;
            }

            #endregion

        }

        
        

        //methods to convert input struct to byte value

        static byte DirectionOffset = 16;

        public static byte ConvertToByte(GameInputStruct Input)
        {
            byte value = 0;
            switch (Input.Direction)
            {   //based on direction + button combination, calculate a byte value
                case Direction.None: { value = (byte)(0 + GetButtonDownValue(Input)); break; }
                case Direction.Up: { value = (byte)(DirectionOffset * 1 + GetButtonDownValue(Input)); break; }
                case Direction.UpRight: { value = (byte)(DirectionOffset * 2 + GetButtonDownValue(Input)); break; }
                case Direction.Right: { value = (byte)(DirectionOffset * 3 + GetButtonDownValue(Input)); break; }
                case Direction.DownRight: { value = (byte)(DirectionOffset * 4 + GetButtonDownValue(Input)); break; }
                case Direction.Down: { value = (byte)(DirectionOffset * 5 + GetButtonDownValue(Input)); break; }
                case Direction.DownLeft: { value = (byte)(DirectionOffset * 6 + GetButtonDownValue(Input)); break; }
                case Direction.Left: { value = (byte)(DirectionOffset * 7 + GetButtonDownValue(Input)); break; }
                case Direction.UpLeft: { value = (byte)(DirectionOffset * 8 + GetButtonDownValue(Input)); break; }
                default: { break; } //max = 16 * 8 = 128
            }
            return value;
        }

        public static byte GetButtonDownValue(GameInputStruct Input)
        {   //check for single button presses
            if (!Input.A && !Input.B && !Input.X && !Input.Y && !Input.Start) { return 0; }
            else if (Input.A && !Input.B && !Input.X && !Input.Y && !Input.Start) { return 1; }
            else if (!Input.A && Input.B && !Input.X && !Input.Y && !Input.Start) { return 2; }
            else if (!Input.A && !Input.B && Input.X && !Input.Y && !Input.Start) { return 3; }
            else if (!Input.A && !Input.B && !Input.X && Input.Y && !Input.Start) { return 4; }
            else if (!Input.A && !Input.B && !Input.X && !Input.Y && Input.Start) { return 5; }
            //check for two button presses starting with A
            else if (Input.A && Input.B && !Input.X && !Input.Y && !Input.Start) { return 6; }
            else if (Input.A && !Input.B && Input.X && !Input.Y && !Input.Start) { return 7; }
            else if (Input.A && !Input.B && !Input.X && Input.Y && !Input.Start) { return 8; }
            else if (Input.A && !Input.B && !Input.X && !Input.Y && Input.Start) { return 9; }
            //check for two button presses starting with B
            else if (!Input.A && Input.B && Input.X && !Input.Y && !Input.Start) { return 10; }
            else if (!Input.A && Input.B && !Input.X && Input.Y && !Input.Start) { return 11; }
            else if (!Input.A && Input.B && !Input.X && !Input.Y && Input.Start) { return 12; }
            //check for two button presses starting with X
            else if (!Input.A && !Input.B && Input.X && Input.Y && !Input.Start) { return 13; }
            else if (!Input.A && !Input.B && Input.X && !Input.Y && Input.Start) { return 14; }
            //check for two button presses starting with Y
            else if (!Input.A && !Input.B && !Input.X && Input.Y && Input.Start) { return 15; }
            return 0;
        }

        //methods to convert byte value to input struct

        public static GameInputStruct ConvertToInputStruct(byte Input)
        {
            GameInputStruct GIS = new GameInputStruct();
            //reduce input value to button value, set buttons
            byte dirCount = 0, btnID = Input;
            while (btnID >= DirectionOffset) { btnID -= DirectionOffset; dirCount++; }
            SetButtons(ref GIS, btnID);
            //determine direction to apply based on number of reductions
            switch (dirCount)
            {
                case 0: { break; }
                case 1: { GIS.Direction = Direction.Up; break; }
                case 2: { GIS.Direction = Direction.UpRight; break; }
                case 3: { GIS.Direction = Direction.Right; break; }
                case 4: { GIS.Direction = Direction.DownRight; break; }
                case 5: { GIS.Direction = Direction.Down; break; }
                case 6: { GIS.Direction = Direction.DownLeft; break; }
                case 7: { GIS.Direction = Direction.Left; break; }
                case 8: { GIS.Direction = Direction.UpLeft; break; }
                default: { break; }
            }
            return GIS;
        }

        public static void SetButtons(ref GameInputStruct GIS, byte btnID)
        {   //check for single button presses
            switch (btnID)
            {   //check for single button presses
                case 0: { break; }
                case 1: { GIS.A = true; break; }
                case 2: { GIS.B = true; break; }
                case 3: { GIS.X = true; break; }
                case 4: { GIS.Y = true; break; }
                case 5: { GIS.Start = true; break; }
                //check for two button presses starting with A
                case 6: { GIS.A = true; GIS.B = true; break; }
                case 7: { GIS.A = true; GIS.X = true; break; }
                case 8: { GIS.A = true; GIS.Y = true; break; }
                case 9: { GIS.A = true; GIS.Start = true; break; }
                //check for two button presses starting with B
                case 10: { GIS.B = true; GIS.X = true; break; }
                case 11: { GIS.B = true; GIS.Y = true; break; }
                case 12: { GIS.B = true; GIS.Start = true; break; }
                //check for two button presses starting with X
                case 13: { GIS.X = true; GIS.Y = true; break; }
                case 14: { GIS.X = true; GIS.Start = true; break; }
                //check for two button presses starting with Y
                case 15: { GIS.Y = true; GIS.Start = true; break; }
            }
        }




        //button PRESS methods

        public static bool IsNewButtonPress_A()
        { return (thisFrame.A && !prevFrame.A); }

        public static bool IsNewButtonPress_B()
        { return (thisFrame.B && !prevFrame.B); }

        public static bool IsNewButtonPress_X()
        { return (thisFrame.X && !prevFrame.X); }

        public static bool IsNewButtonPress_Y()
        { return (thisFrame.Y && !prevFrame.Y); }

        public static bool IsNewButtonPress_Start()
        { return (thisFrame.Start && !prevFrame.Start); }

        public static bool IsNewButtonPress_ANY()
        {
            if (IsNewButtonPress_A() ||
                IsNewButtonPress_B() ||
                IsNewButtonPress_X() ||
                IsNewButtonPress_Y() ||
                IsNewButtonPress_Start())
            { return true; }
            else { return false; }
        }

        //button HELD methods

        public static bool IsNewButtonHeld_A()
        { return (thisFrame.A && prevFrame.A); }

        public static bool IsNewButtonHeld_B()
        { return (thisFrame.B && prevFrame.B); }

        public static bool IsNewButtonHeld_X()
        { return (thisFrame.X && prevFrame.X); }

        public static bool IsNewButtonHeld_Y()
        { return (thisFrame.Y && prevFrame.Y); }

        public static bool IsNewButtonHeld_Start()
        { return (thisFrame.Start && prevFrame.Start); }



    }
}