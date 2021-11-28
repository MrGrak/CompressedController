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


        public static short InputRecBufferSize = 1000; //1k for challenge
        public static GameInputStruct[] Rec_GISBuffer = new GameInputStruct[InputRecBufferSize];
        public static byte[] Rec_ByteBuffer = new byte[InputRecBufferSize];



        public static void Constructor(GraphicsDeviceManager gdm, SpriteBatch sb, Game g)
        {
            //read data into Rec_GISBuffer
            ReadGamePadStructFile();

            //iterate and map/compress buffer into byte array
            for(int i = 0; i < InputRecBufferSize; i++)
            { Rec_ByteBuffer[i] = ConvertToByte(Rec_GISBuffer[i]); }

            //write byte buffer to file, for comparison
            WriteByteArray();

            //exit game, we're done
            g.Exit();
        }

        public static void Update() { }

        public static void Draw() { }





        //methods for loading challenge data

        public static void ReadGamePadStructFile()
        {
            string dir = Path.Combine(GetExecutingDirectoryName(), "GamePadStruct.bin");
            byte[] data = File.ReadAllBytes(dir);

            using (MemoryStream stream = new MemoryStream(data))
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    for (int i = 0; i < InputRecBufferSize; i++)
                    {
                        GameInputStruct G = new GameInputStruct();
                        G.Direction = (Direction)reader.ReadByte();
                        G.Start = reader.ReadBoolean();
                        G.A = reader.ReadBoolean();
                        G.B = reader.ReadBoolean();
                        G.X = reader.ReadBoolean();
                        G.Y = reader.ReadBoolean();
                        Rec_GISBuffer[i] = G;
                    }
                    reader.Close();
                    stream.Close();
                }
            }
        }

        public static string GetExecutingDirectoryName()
        {
            var location = new Uri(Assembly.GetEntryAssembly().GetName().CodeBase);
            return new FileInfo(location.AbsolutePath).Directory.FullName;
        }




        //encoding/conversion methods

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

        public static void WriteByteArray()
        {
            string dir = Path.Combine(GetExecutingDirectoryName(), "GamePadByte.bin");
            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {   //write all bytes from buffer into stream
                    for (int i = 0; i < InputRecBufferSize; i++)
                    { writer.Write((byte)Rec_ByteBuffer[i]); }
                    var data = stream.ToArray();

                    //data could be compressed here...

                    //write byte array to game dir
                    using (var s = File.Open(dir, FileMode.Create, FileAccess.Write))
                    { s.Write(data, 0, data.Length); }
                }
            }
        }





    }
}