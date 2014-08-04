OgreFlow
=============

A simple .NET based particle editor for the Ogre3D Engine

=============

It's hard to create particle effects in Ogre 3D engine without seeing the final results in real-time. Going back and forth and re-launching the engine every time after making one or couple of corrections to the particle scripts is non-intuitive.

OgreFlow helps to remedy this problem - it is a simple Ogre3D Engine particle configuration editor capable of editing all the standard Ogre 1.7 particle effect parameters and is completely FREE!

### Homepage ###
http://www.wzona.info/p/ogre-flow-particle-editor.html

### Current version ###
  Ogre Flow, version 1.0, with full C# source
  
### Main features ###
  * Can edit all standard Ogre 1.7.x particle parameters
  * WYSIWYG style editing
  * Ability to export created systems to Ogre .particle script format
  * Multi-system multi-emitter systems can be created
  * Simple user interface 
  * Easy to read/modify/customize XML file formats
  * Ability to include different & own materials

### Requirements ###
  * Microsoft .NET framework installed. 
  * Ogre capable graphics card

### Usage ###
1. Run the Flow.exe inside the 'bin/Release' directory.
2. At startup select your Ogre render window configuration (to change next time - delete "ogre.cfg" file)
3. Add a particle system & an emitter to start crafting effects 
4. Enjoy! If you find any bugs, please report them to: qverdi [et] hotmail [dot] com

### Samples ###
Available in: `Samples` folder.

### Known bugs ###
1. Error when inserting particle system after couple of resets using File->New
2. Some emitter markers are left after deleting/reseting the scene. 
3. Windows 7 x64 crash on application startup

### OgreFlow in action ###

Waterfall, simple fire & snow example

![Alt text](/web/screen1_thumb.png "Waterfall, simple fire & snow example")

Rain & fountain example

![Alt text](/web/screen2_thumb.png "Rain & fountain example")

Dense smoke example

![Alt text](/web/shot1_thumb.png "Dense smoke example")
