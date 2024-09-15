# RobotGame
Port of XNA RobotGame Sample to MonoGame/FNA
![image](https://github.com/user-attachments/assets/312c7399-f7e3-478c-967d-db9f30f2841f)
The port doesn't use Content Pipeline, but loads all assets in raw form using [XNAssets](https://github.com/rds1983/XNAssets). All game 3d models are loaded from glb.

## Building From Source Code for MonoGame
Open either RobotGame.MonoGame.WindowsDX.sln or RobotGame.MonoGame.DesktopGL.sln in the IDE and run.
Post screen effects don't work correctly in MG, hence they were turned off.

## Building From Source Code for FNA

Clone following repos in one folder:
* [FNA](https://github.com/FNA-XNA/FNA)
* [DdsKtxXna](https://github.com/rds1983/DdsKtxXna)
* [XNAssets](https://github.com/rds1983/XNAssets)
* [FontStashSharp](https://github.com/FontStashSharp/FontStashSharp)
* This repo

Then simply open RacingGame.FNA.Core.sln in the IDE and run.

# Credits
* [Original game source for XNA 4](https://www.moddb.com/downloads/xna-40-robot-game)
* [XNA for VS 2017](https://github.com/SimonDarksideJ/XNAGameStudio)
* [Instruction on how to install XNA for VS 2017](https://gist.github.com/roy-t/2f089414078bf7218350e8c847951255)
* [FNA](https://fna-xna.github.io/)
* [MonoGame](https://monogame.net/)
