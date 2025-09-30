using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using Quintessential;
using Quintessential.Settings;
using SDL2;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using YamlDotNet.Core.Tokens;

namespace Devmodeus;

using PartType = class_139;
using Permissions = enum_149;
using PartTypes = class_191;
using Texture = class_256;

public static class StrokeBuilder
{
    //
    private static Texture[] StrokePieces = new Texture[16];

    public static void LoadPuzzleContent()
    {
        for (int i = 1; i < 15; i++)
        {
            // textures are arranged in a very specific order
            // that makes it simple to do look-up-table stuff
            StrokePieces[i] = class_235.method_615("devmodeus/textures/select/stroke_pieces/" + i);
        }
    }

    public const float boxWidth = 82f;
    public const float boxHeight = 71f;
    public static readonly Vector2 boxSize = new Vector2(boxWidth, boxHeight);
    public static Vector2 HexToPosition(HexIndex hex) => new Vector2((hex.Q + 0.5f * hex.R) * boxWidth, hex.R * boxHeight);

    public static Vector2 ComponentwiseMin(Vector2 u, Vector2 v) => new Vector2(Math.Min(u.X, v.X), Math.Min(u.Y, v.Y));
    public static Vector2 ComponentwiseMax(Vector2 u, Vector2 v) => new Vector2(Math.Max(u.X, v.X), Math.Max(u.Y, v.Y));

    public static Texture BuildStrokeFromHexes(HashSet<HexIndex> coveredHexes)
    {
        Texture stroke = class_238.field_1989.field_73; // single transparent pixel
        if (coveredHexes.Count == 0) return stroke;

        HexIndex left = new HexIndex(-1, 0);
        HexIndex downleft = new HexIndex(0, -1);
        HexIndex downright = new HexIndex(1, -1);

        // preprocess
        Dictionary<HexIndex, int> texIDs = new();
        foreach (var hex in coveredHexes)
        {
            texIDs[hex + left] = 0;
            texIDs[hex + downleft] = 0;
            texIDs[hex + downright] = 0;
            texIDs[hex] = 0;
        }

        // process
        Vector2 bottomLeft = HexToPosition(coveredHexes.First());
        Vector2 topRight = bottomLeft + boxSize;
        foreach (var hex in coveredHexes)
        {
            // ID calculations
            texIDs[hex + left] += 1;
            texIDs[hex + downleft] += 2;
            texIDs[hex + downright] += 4;
            texIDs[hex] += 8;

            // update texture bounds
            bottomLeft = ComponentwiseMin(bottomLeft, HexToPosition(hex + left));
            bottomLeft = ComponentwiseMin(bottomLeft, HexToPosition(hex + downright));
            topRight = ComponentwiseMax(topRight, HexToPosition(hex + left) + boxSize);
            topRight = ComponentwiseMax(topRight, HexToPosition(hex + downright) + boxSize);
        }

        // draw texture within renderTarget
        RenderTargetHandle renderTargetHandle = new RenderTargetHandle();

        // need to fix how we generate bounds and draw textures to the render target so we don't cut them off
        // for now, we hack it so we get the whole texture
        Vector2 debug_getBackOnScreen = new Vector2(0, 200);

        Bounds2 bounds = Bounds2.WithCorners(bottomLeft, topRight + debug_getBackOnScreen);
        Index2 textureSize = bounds.Size.CeilingToInt();
        renderTargetHandle.field_2987 = textureSize;
        class_95 class95 = renderTargetHandle.method_1352(out var flag);
        if (flag)
        {
            Vector2 origin = new Vector2(bottomLeft.X, -bottomLeft.Y + debug_getBackOnScreen.Y * 0.5f); // need to invert Y so stuff actually appears in drawing target because of drawing weirdness
            var scaling = 1f;
            var matrixRotateGraphic = Matrix4.method_1073(0f);
            var matrixPivotOffset = Matrix4.method_1070(Vector2.Zero.ToVector3(0f));
            var matrixScaling = Matrix4.method_1074(scaling * new Vector2(1, 1).ToVector3(0f));


            using (class_226.method_597(class95, Matrix4.method_1074(new Vector3(1,-1,1)))) // need to flip Y, otherwise stuff is drawn upside (wtf)
            {
                // draw background
                class_226.method_600(Color.Transparent);
                // draw stroke pieces
                foreach (var kvp in texIDs)
                {
                    var hex = kvp.Key;
                    var ID = kvp.Value;
                    if (ID > 0 && ID < 15)
                    {
                        var texture = StrokePieces[ID];
                        var size = texture.field_2056;
                        var position = HexToPosition(hex) - origin;
                        var matrixScreenPosition = Matrix4.method_1070(position.ToVector3(0f));
                        var matrixTextureSize = Matrix4.method_1074(size.ToVector3(0f));
                        var matrix = matrixScreenPosition * matrixRotateGraphic * matrixPivotOffset * matrixScaling * matrixTextureSize;
                        class_135.method_262(texture, Color.White, matrix);
                    }
                }
            }
        }
        stroke = renderTargetHandle.method_1351().field_937;
        return stroke;
    }

    public static void SaveTextureToFile(Texture texture, string dir, string filepath)
    {
        string fullPath = Path.Combine(dir, filepath + ".png");
        Renderer.method_1313(texture).method_735(fullPath);
    }

    public static void DumpGlyphStrokes()
    {
        string outDir = Path.Combine(MainClass.PathModSaves, "Devmodeus", "DumpedGlyphStrokes");
        Directory.CreateDirectory(outDir);
        var sound = class_238.field_1991.field_1872; // 'sounds/ui_modal'
        sound.method_28(0.2f);

        foreach (var glyph in PartTypes.field_1785.Where(x => x.field_1540.Count() > 0))
        {
            string name = glyph.field_1528;
            var hexes = glyph.field_1540;
            SaveTextureToFile(BuildStrokeFromHexes(new HashSet<HexIndex>(hexes)), outDir, name + "_stroke");
        }

        UI.OpenScreen(new NoticeScreen("Texture Dumping", $"Saved stroke textures to \"{outDir.Replace('\\', '/')}\""));

        // the resulting textures are darker than they should be? not sure why the output is darker than the component pieces...
        // also, some glyphs (like purification) are getting drawn out-of-frame - need to figure out texture rendering better
    }
}