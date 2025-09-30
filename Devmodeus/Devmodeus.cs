using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using Quintessential;
using Quintessential.Settings;
using SDL2;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Xml.Serialization;
using YamlDotNet.Serialization;
using System.IO;

namespace Devmodeus;

//using PartType = class_139;
//using Permissions = enum_149;
//using BondType = enum_126;
//using BondSite = class_222;
//using Bond = class_277;
//using AtomTypes = class_175;
//using PartTypes = class_191;
//using Texture = class_256;
public class MainClass : QuintessentialMod
{
	public static MethodInfo PrivateMethod<T>(string method) => typeof(T).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
	public override Type SettingsType => typeof(MySettings);
	public static QuintessentialMod MainClassAsMod;
    public static string PathModSaves = "";

	//public static bool PressedKeyBinding() => MySettings.Instance.keyBinding.Pressed();
	public class MySettings
	{
		public static MySettings Instance => MainClassAsMod.Settings as MySettings;
        /*
		[SettingsLabel("Boolean Setting")]
		public bool booleanSetting = true;

		[SettingsLabel("KeyBinding")]
		public Keybinding keyBinding = new() { Key = "W" };
        */

        [SettingsLabel("Dump Glyph Strokes")]
        [YamlIgnore]
        public SettingsButton DumpGlyphStrokes = StrokeBuilder.DumpGlyphStrokes;
    }
    public override void ApplySettings()
	{
		base.ApplySettings();
		var SET = (MySettings)Settings;
		//var booleanSetting = SET.booleanSetting;
	}
	public override void Load()
	{
		MainClassAsMod = this;
		Settings = new MySettings();
	}
	public override void LoadPuzzleContent()
	{
        StrokeBuilder.LoadPuzzleContent();
	}
	public override void Unload()
	{
		//
	}
	public override void PostLoad()
	{
        PathModSaves = Path.Combine(class_161.method_402(), "ModSettings");
        Logger.Log($"Mod settings directory: \"{PathModSaves}\"");
        if (!Directory.Exists(PathModSaves)) Directory.CreateDirectory(PathModSaves);
    }
}
