﻿using Terraria.ModLoader;
using Terraria;
using Terraria.GameContent.Generation;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria.WorldBuilding;
using Terraria.IO;

namespace WorldGenPreviewer
{
	public class WorldGenPreviewer : Mod
	{
		internal static WorldGenPreviewer instance;

		public override void Load()
		{
			instance = this;
		}

		public override void Unload()
		{
			instance = null;
			Main.skipMenu = false;
		}
	}

	internal class WorldGenPreviewerModWorld : ModSystem
	{
		internal static bool saveLockForced = false;
		internal static bool continueWorldGen = true;
		internal static bool pauseAfterContinue = false;
		internal static bool repeatPreviousStep = false;
		internal static List<GenPass> generationPasses;

		public override void OnWorldLoad()
		{
			if (saveLockForced)
			{
				saveLockForced = false;
				Main.skipMenu = false;
			}
		}

		public override void ModifyWorldGenTasks(List<GenPass> tasks, ref float totalWeight)
		{
			generationPasses = tasks;
			// Reset Terrain
			// Reset Special Terrain
			// or after reset
			Main.skipMenu = false; // Reset skipMenu to false so that worlds can be saved
			int ResetStepIndex = tasks.FindIndex(genpass => genpass.Name.Equals("Reset"));
			if (ResetStepIndex != -1)
			{
				tasks.Insert(ResetStepIndex + 1, new PassLegacy("Special World Gen Progress", delegate (GenerationProgress progress, GameConfiguration config)
				{
					Main.FixUIScale();
					progress.Message = "Setting up Special World Gen Progress";
					Main.refreshMap = true;
					var a = new UIWorldLoadSpecial(progress);

					Main.MenuUI.SetState(a);

					Main.updateMap = false;
					Main.mapStyle = 0;
					Main.mapFullscreen = true;
					Main.mapFullscreenScale = Main.screenWidth / (float)Main.maxTilesX * 0.8f;
					Main.mapMinX = 0;
					Main.mapMinY = 0;
					Main.mapMaxX = Main.maxTilesX;
					Main.mapMaxY = Main.maxTilesY;
					Main.mapFullscreenPos = new Vector2(Main.maxTilesX / 2, Main.maxTilesY / 2);
					Main.mapReady = true;
				}));

				// Reset Special Paused Terrain Paused ...
				for (int i = tasks.Count - 1; i >= ResetStepIndex + 2; i--)
				{
					string name = tasks[i - 1].Name;
					GenPass previous = tasks[i - 1];
					GenPass next = tasks[i];
					tasks.Insert(i, new PassLegacy("World Gen Paused", delegate (GenerationProgress progress, GameConfiguration config)
					{
						UIWorldLoadSpecial.BadPass = next.Name == "Expand World";

						foreach (var item in UIWorldLoadSpecial.instance.passesList._items)
						{
							UIPassItem passitem = item as UIPassItem;
							if (passitem.pass == previous)
							{
								passitem.Complete();
								break;
							}
						}
						if (!continueWorldGen)
						{
							progress.Message = "World Gen Paused after " + name;
							UIWorldLoadSpecial.instance.statusLabel.SetText("Status: Paused");
						}
						while (true)
						{
							if (repeatPreviousStep)
							{
								repeatPreviousStep = false;
								//string previousStatus = UIWorldLoadSpecial.instance.statusLabel.SetText
								UIWorldLoadSpecial.instance.statusLabel.SetText("Status: Doing Previous Step Again");
								previous.Apply(progress, config);
								//if (continueWorldGen)
								//{
								//	UIWorldLoadSpecial.instance.statusLabel.SetText("Status: Normal");
								//}
								//else
								//{
								UIWorldLoadSpecial.instance.statusLabel.SetText("Status: Paused");
								//}
							}
							if (continueWorldGen)
							{
								if (pauseAfterContinue)
								{
									pauseAfterContinue = false;
									continueWorldGen = false;
								}
								break;
							}
						}
					}));
				}
			}
			else
			{
				Mod.Logger.Error("WorldGenPreviewer mod unable to do it's thing since someone removed reset step");
			}
		}

		public override void PostWorldGen()
		{
			// reset map to original
			Main.mapFullscreen = false;
			Main.mapStyle = 1;
			//structures_structures = null;
		}

		/* TODO: enable this via a toggle button
		internal static List<Rectangle> structures_structures; // reference to WorldGen.structures._structures
		public override void PreWorldGen()
		{
			FieldInfo structuresField = typeof(StructureMap).GetField("_structures", BindingFlags.Instance | BindingFlags.NonPublic);
			structures_structures = (List<Rectangle>)structuresField.GetValue(WorldGen.structures);
		}
		*/
	}
}
