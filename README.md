# CompressedController
An example of reducing a gamepad's state to a single byte, for recording or network purposes

In this example, an input class is used to map a gamepad state to a very simple UI, which displays directional input along with ABXY and Start buttons. Input is mapped from the gamepad state to a game input struct, which is then mapped to a single byte value, representing the compressed state. This all done using a circular buffer (an array of bytes), so that the example can toggle between recording and playing back recorded input. Here is what the example looks like:

![](https://github.com/MrGrak/CompressedController/tree/main/Gifs/compressedController_001.gif) 