# CompressedController
An example (not a library) of reducing a gamepad's state to a single byte, for recording or network purposes.

In this example, an input class is used to map a gamepad state to a very simple UI, which displays directional input along with ABXY and Start buttons. Input is mapped from the gamepad state to a game input struct, which is then mapped to a single byte value, representing the compressed state. This all done using a circular buffer (an array of bytes), so that the example can toggle between recording and playing back recorded input. Here is what the example looks like:

![](https://github.com/MrGrak/CompressedController/blob/main/Gifs/compressedController_001.gif)

Red represents recording state - code is recording gamepad input to buffer.  
Green represents playback state - code is playing back input from buffer.  

What's useful:  
Reducing the controller state to a single byte, which can make input recording files smaller, and possibly network packets containing gamepad state. In this example, the input buffer is 600 bytes in size, plus the heap allocated array object data.

Issues:  
In order to fit the gamepad state into a byte, some states have to be discarded. I have chosen to discard controller states where there are more than 2 buttons and a direction pressed down. I have also chosen to simplify controller joystick input from a Vector2 value to a single byte direction. So, there are various limitations that apply to this strategy. In the future, I might migrate the controller state to use two bytes, one for direction input and one for button input, which would allow for more states, which addresses the issues noted here. 