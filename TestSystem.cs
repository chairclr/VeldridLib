using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace VeldridLib;

public class TestSystem : ModSystem
{
    public override void PostDrawTiles()
    {
        GraphicsDeviceManager gdm = Main.graphics;

        if (gdm is not null)
        {
            GraphicsDevice gd = gdm.GraphicsDevice;

            Veldrid.GraphicsDevice vgd = FNAReflector.GetGraphicsDevice(gd);
        }
    }
}