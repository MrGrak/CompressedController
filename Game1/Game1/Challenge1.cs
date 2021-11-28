using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Game1
{
    public static class Challenge1
    {
        public enum Direction : byte
        {   //represents possible controller directions  
            None,
            Up, UpRight, Right, DownRight, Down, DownLeft, Left, UpLeft
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

        public static Game G;
        public static GraphicsDeviceManager GDM;
        public static SpriteBatch SB;

        //4 recs for direction pad, 4 recs for abxy, 1 for start
        public static byte Size = 9;
        public static Texture2D Texture;
        public static List<Rectangle> Rectangles;

        //structs to store this frame and prev frames input
        public static GameInputStruct thisFrame;

        public static GamePadState ControllerState;
        //the amount of joystick movement classified as noise
        public static float deadzone = 0.20f;

        //stores gamepad state in a byte
        public static byte CompressedState;
        //allocate a buffer of game input structs to write to - 60 fps x N seconds
        public static short InputRecBufferSize = 1000; //1k for challenge
        public static short InputRecordingCounter = 0;

        //setup the recording input buffers
        public static byte[] Rec_ByteBuffer = new byte[InputRecBufferSize];
        public static GameInputStruct[] Rec_GISBuffer = new GameInputStruct[InputRecBufferSize];
        public static GamePadState[] Rec_GPSBuffer = new GamePadState[InputRecBufferSize];
        
        //controls if class is recording or playing back input
        public static bool Recording = true;

        //size values
        public static int size_GPS; //56 bytes
        public static int size_GIS; //24 bytes



        public static void Constructor(GraphicsDeviceManager gdm, SpriteBatch sb, Game g)
        {
            //store references to what this class needs to function
            G = g; GDM = gdm; SB = sb;
            //create a tiny texture to draw with + tint
            Texture = new Texture2D(GDM.GraphicsDevice, 1, 1);
            Texture.SetData<Color>(new Color[] { Color.White });
            CompressedState = 0; //reset controller state

            //place rectangles in controller positions
            Point Pos = new Point(75, 50); //controls overall ui position
            byte scale = 25;
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
                    case 8: { Rectangles.Add(new Rectangle(Pos.X + scale * 3, Pos.Y + scale, scale * 2, scale)); break; } //start button

                    default: { break; }
                }
            }

            //get size of the gamepad state and game input struct for comparison
            size_GPS = System.Runtime.InteropServices.Marshal.SizeOf(typeof(GamePadState));
            size_GIS = System.Runtime.InteropServices.Marshal.SizeOf(typeof(GameInputStruct));
        }

        public static void Update()
        {
            if(Recording)
            {
                //map controller to input struct, store in input buffer
                
                //clear this frame's input
                thisFrame = new GameInputStruct();
                //assume player one, this is brittle
                ControllerState = GamePad.GetState(PlayerIndex.One);
                //record gamepad state state to recording buffer
                Rec_GPSBuffer[InputRecordingCounter] = ControllerState;

                //map controller state to struct - useful when mapping diff inputs (like keys)
                MapController(ref ControllerState, ref thisFrame);
                //record 24 byte controller state to recording buffer
                Rec_GISBuffer[InputRecordingCounter] = thisFrame;

                //convert input struct to byte, record in buffer
                Rec_ByteBuffer[InputRecordingCounter] = ConvertToByte(thisFrame);
                //update compressed state that we draw later
                CompressedState = Rec_ByteBuffer[InputRecordingCounter];
                
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
                //write recorded input to local binary files for size comparison
                

                #region Write input byte array

                {
                    string dir = Path.Combine(GetExecutingDirectoryName(), "GamePadByte.bin");
                    using (MemoryStream stream = new MemoryStream())
                    {
                        using (BinaryWriter writer = new BinaryWriter(stream))
                        {   //write all bytes from buffer into stream
                            for (int i = 0; i < InputRecBufferSize; i++)
                            { writer.Write((byte)Rec_ByteBuffer[i]); }
                            //convert to byte array
                            var data = stream.ToArray();
                            //write byte array to game dir
                            using (var s = File.Open(dir, FileMode.Create, FileAccess.Write))
                            { s.Write(data, 0, data.Length); }
                        }
                    }
                }

                #endregion

                #region Write input game pad struct array

                {
                    string dir = Path.Combine(GetExecutingDirectoryName(), "GamePadStruct.bin");
                    using (MemoryStream stream = new MemoryStream())
                    {
                        using (BinaryWriter writer = new BinaryWriter(stream))
                        {   //write all structs from buffer into stream
                            for (int i = 0; i < InputRecBufferSize; i++)
                            {
                                writer.Write((byte)Rec_GISBuffer[i].Direction);
                                writer.Write((bool)Rec_GISBuffer[i].Start);
                                writer.Write((bool)Rec_GISBuffer[i].A);
                                writer.Write((bool)Rec_GISBuffer[i].B);
                                writer.Write((bool)Rec_GISBuffer[i].X);
                                writer.Write((bool)Rec_GISBuffer[i].Y);
                            }
                            //convert to byte array
                            var data = stream.ToArray();
                            //write byte array to game dir
                            using (var s = File.Open(dir, FileMode.Create, FileAccess.Write))
                            { s.Write(data, 0, data.Length); }
                        }
                    }
                }

                #endregion

                #region Write gamepad state struct array

                {
                    string dir = Path.Combine(GetExecutingDirectoryName(), "GamePadState.bin");
                    using (MemoryStream stream = new MemoryStream())
                    {
                        using (BinaryWriter writer = new BinaryWriter(stream))
                        {
                            //write all structs from buffer into stream
                            for (int i = 0; i < InputRecBufferSize; i++)
                            {
                                //ButtonState is defined as an int enum
                                writer.Write((int)Rec_GPSBuffer[i].Buttons.A);
                                writer.Write((int)Rec_GPSBuffer[i].Buttons.B);
                                writer.Write((int)Rec_GPSBuffer[i].Buttons.X);
                                writer.Write((int)Rec_GPSBuffer[i].Buttons.Y);
                                writer.Write((int)Rec_GPSBuffer[i].Buttons.Back);
                                writer.Write((int)Rec_GPSBuffer[i].Buttons.BigButton);
                                writer.Write((int)Rec_GPSBuffer[i].Buttons.LeftShoulder);
                                writer.Write((int)Rec_GPSBuffer[i].Buttons.LeftStick);
                                writer.Write((int)Rec_GPSBuffer[i].Buttons.RightShoulder);
                                writer.Write((int)Rec_GPSBuffer[i].Buttons.RightStick);
                                writer.Write((int)Rec_GPSBuffer[i].Buttons.Start);
                                writer.Write((int)Rec_GPSBuffer[i].DPad.Down);
                                writer.Write((int)Rec_GPSBuffer[i].DPad.Left);
                                writer.Write((int)Rec_GPSBuffer[i].DPad.Right);
                                writer.Write((int)Rec_GPSBuffer[i].DPad.Up);
                                writer.Write((bool)Rec_GPSBuffer[i].IsConnected);
                                writer.Write((int)Rec_GPSBuffer[i].PacketNumber);
                                //these are all float values
                                writer.Write((float)Rec_GPSBuffer[i].ThumbSticks.Left.X);
                                writer.Write((float)Rec_GPSBuffer[i].ThumbSticks.Left.Y);
                                writer.Write((float)Rec_GPSBuffer[i].ThumbSticks.Right.X);
                                writer.Write((float)Rec_GPSBuffer[i].ThumbSticks.Right.Y);
                                writer.Write((float)Rec_GPSBuffer[i].Triggers.Left);
                                writer.Write((float)Rec_GPSBuffer[i].Triggers.Right);
                            }
                            //convert to byte array
                            var data = stream.ToArray();
                            //write byte array to game dir
                            using (var s = File.Open(dir, FileMode.Create, FileAccess.Write))
                            { s.Write(data, 0, data.Length); }
                        }
                    }
                }

                #endregion

                

                //done, exit game
                G.Exit();
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

        public static string GetExecutingDirectoryName()
        {
            var location = new Uri(Assembly.GetEntryAssembly().GetName().CodeBase);
            return new FileInfo(location.AbsolutePath).Directory.FullName;
        }

        
        //methods to convert input struct to byte value

        static byte DirectionOffset = 17;

        public static byte ConvertToByte(GameInputStruct Input)
        {
            byte value = 0;
            switch (Input.Direction)
            {   //based on direction + button combination, calculate a byte value
                //based on 9 directions, plus 17 button combinations = 153 total states
                case Direction.None: { value = (byte)(0 + GetButtonDownValue(Input)); break; }
                case Direction.Up: { value = (byte)(DirectionOffset * 1 + GetButtonDownValue(Input)); break; }
                case Direction.UpRight: { value = (byte)(DirectionOffset * 2 + GetButtonDownValue(Input)); break; }
                case Direction.Right: { value = (byte)(DirectionOffset * 3 + GetButtonDownValue(Input)); break; }
                case Direction.DownRight: { value = (byte)(DirectionOffset * 4 + GetButtonDownValue(Input)); break; }
                case Direction.Down: { value = (byte)(DirectionOffset * 5 + GetButtonDownValue(Input)); break; }
                case Direction.DownLeft: { value = (byte)(DirectionOffset * 6 + GetButtonDownValue(Input)); break; }
                case Direction.Left: { value = (byte)(DirectionOffset * 7 + GetButtonDownValue(Input)); break; }
                case Direction.UpLeft: { value = (byte)(DirectionOffset * 8 + GetButtonDownValue(Input)); break; }
                default: { break; } 
            }
            return value;
        }
        
        public static byte GetButtonDownValue(GameInputStruct Input)
        {   
            //check for single button presses
            if (!Input.A && !Input.B && !Input.X && !Input.Y && !Input.Start) { return 0; }
            else if (Input.A && !Input.B && !Input.X && !Input.Y && !Input.Start) { return 1; }
            else if (!Input.A && Input.B && !Input.X && !Input.Y && !Input.Start) { return 2; }
            else if (!Input.A && !Input.B && Input.X && !Input.Y && !Input.Start) { return 3; }
            else if (!Input.A && !Input.B && !Input.X && Input.Y && !Input.Start) { return 4; }
            else if (Input.Start) { return 5; } //special case for start
            //check for two button presses starting with A
            else if (Input.A && Input.B && !Input.X && !Input.Y) { return 6; }
            else if (Input.A && !Input.B && Input.X && !Input.Y) { return 7; }
            else if (Input.A && !Input.B && !Input.X && Input.Y) { return 8; }
            //check for two button presses starting with B
            else if (!Input.A && Input.B && Input.X && !Input.Y) { return 9; }
            else if (!Input.A && Input.B && !Input.X && Input.Y) { return 10; }
            //check for two button presses starting with X
            else if (!Input.A && !Input.B && Input.X && Input.Y) { return 11; }
            //check for three button presses
            else if (Input.A && Input.B && Input.X && !Input.Y) { return 12; }
            else if (Input.A && Input.B && !Input.X && Input.Y) { return 13; }
            else if (!Input.A && Input.B && Input.X && Input.Y) { return 14; }
            else if (Input.A && !Input.B && Input.X && Input.Y) { return 15; }
            //check for 4 buttons down
            else if (Input.A && Input.B && Input.X && Input.Y) { return 16; }
            //total of 17 unique button combinations
            return 0;
        }
        

    }
}