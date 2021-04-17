# vizzy
Binary file visualization app in C# 


<img src="readme/readme0.png" width="400"/>

## Things you can do
* Drag & drop your file in the hex editor.
* Click on the hex editor to offset the visualization.
* Ctrl + wheel over the visualization to zoom.
* Wheel & shift + wheel over the visualization to scroll.
* Click on the visualization to move the hex editor to that offset.
* Wheel over the columns input to change it by 1, or click the buttons to double and halve it.
* Select color depth and pixel format.
* For sub-byte color depth, toggle between LSB0 and MSB0 bit endianness. MSB0 doesn't support an arbitrary number of columns.
* Toggle between black and transparent background.
* Save the visualization as bitmap.


## Things you cannot do
* Cannot view past 32768 rows from the current offset, but you can save images taller than that and view them in your image viewer.
## Download
[Version 0.4.1](https://github.com/nickbrick/vizzy/releases/tag/v0.4.1) Windows 32 & 64 bit
