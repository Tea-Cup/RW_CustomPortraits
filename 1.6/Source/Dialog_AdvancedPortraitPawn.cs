using System;
using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Foxy.CustomPortraits {
	public class Dialog_AdvancedPortraitPawn : Window {
		private readonly Pawn pawn;
		private readonly PawnPortraits storage;
		private readonly List<FloatMenuOption> dropdownOptions = new List<FloatMenuOption>();
		private Vector2 scrollPosition = Vector2.zero;
		private int selected_index = -1;

		private const float PORTRAIT_SIZE = 150;
		private static GUIStyle ItemContainerStyle { get; } = new GUIStyle() {
			normal = new GUIStyleState() { background = null },
			hover = new GUIStyleState() { background = TexUI.TextBGBlack }
		};

		public static readonly List<Type> ConditionTypes = new List<Type>() {
			typeof(ConstantPortraitCondition),
			typeof(MoodPortraitCondition)
		};

		// Height from Dialog_SimplePortraitPawn, adjust in PreOpen later
		public override Vector2 InitialSize => new Vector2(500f, 375f);
		protected override float Margin => 5f;

		public Dialog_AdvancedPortraitPawn(Pawn pawn) {
			optionalTitle = Helper.Label("PortraitFor", pawn);
			doCloseX = true;
			this.pawn = pawn;
			PortraitCache.Update();
			storage = pawn.GetPortraits();
			GenerateConditionOptions();
		}

		public override void PreOpen() {
			base.PreOpen();
			// Bottom edge aligned with the Simple dialog
			// Move top edge up to achieve target height
			windowRect.yMin -= 500f - InitialSize.y;
		}

		public static Dialog_AdvancedPortraitPawn Open(Pawn pawn) {
			Dialog_AdvancedPortraitPawn dialog = new Dialog_AdvancedPortraitPawn(pawn);
			Find.WindowStack.Add(dialog);
			return dialog;
		}

		public override void DoWindowContents(Rect inRect) {
			if (WorldRendererUtility.CurrentWorldRenderMode == WorldRenderMode.Planet) Close();
			inRect.SplitHorizontally(inRect.height - 30, out Rect contentRect, out Rect advRect);
			contentRect.yMax -= 5f;

			GUILayout.BeginArea(contentRect);

			int count = storage.Conditions.Count;
			Rect fullRect = new Rect(0, 0, contentRect.width - 26, count * PORTRAIT_SIZE + (count + 1) * 10);
			Widgets.BeginScrollView(contentRect, ref scrollPosition, fullRect);

			Rect rect = new Rect(0, 0, fullRect.width, PORTRAIT_SIZE);
			float visibleMin = scrollPosition.y;
			float visibleMax = scrollPosition.y + contentRect.height;
			int drawn = 0;

			for (int i = 0; i < count; i++) {
				// If item is below visible window, stop drawing
				if (rect.yMin > visibleMax) break;
				// If item is above visible window, skip drawing
				if (visibleMin < rect.yMax) {
					drawn++;
					DrawEntry(rect, i, storage.Conditions[i]);
					Widgets.DrawLineHorizontal(0, rect.yMax + 5, rect.width);
				}
				rect.y += PORTRAIT_SIZE + 10;
			}

			Widgets.EndScrollView();
			GUILayout.EndArea();

			bool advanced = true;
			Widgets.CheckboxLabeled(advRect, Helper.Label("AdvancedPortrait"), ref advanced);
			if (!advanced) {
				Close();
				StaticSettings.Advanced = false;
				Helper.OpenDialog(pawn);
			}

			if (Find.Selector.SingleSelectedThing is Pawn p && p != pawn) {
				Close();
				Open(p);
			}
		}

		private void DrawEntry(Rect rect, int index, PortraitCondition pc) {
			GUI.Box(rect, "", ItemContainerStyle);
			Rect left = new Rect(rect.xMin, rect.yMin, PORTRAIT_SIZE, rect.height);
			Rect center = new Rect(rect.xMin + PORTRAIT_SIZE + 10, rect.yMin, rect.width - PORTRAIT_SIZE * 2 - 20, rect.height);
			Rect right = new Rect(rect.xMax - PORTRAIT_SIZE, rect.yMin, PORTRAIT_SIZE, rect.height);

			string label = GetTranslationKey(pc.GetType()).Translate();
			if (Widgets.ButtonText(left.TopPartPixels(20), label)) {
				selected_index = index;
				Find.WindowStack.Add(new FloatMenu(dropdownOptions));
			}
			left.yMin += 25;
			pc.Draw(pawn, left, center, right);
		}

		private void OnTypeSelection(Type type) {
			storage.Conditions[selected_index] = (PortraitCondition)Activator.CreateInstance(type);
		}

		private void GenerateConditionOptions() {
			dropdownOptions.Clear();
			foreach (Type t in ConditionTypes) {
				FloatMenuOption option = new FloatMenuOption(
					GetTranslationKey(t).Translate(),
					() => OnTypeSelection(t)
				);
				dropdownOptions.Add(option);
			}
		}
		private static string GetTranslationKey(Type type) {
			FieldInfo fi = type.GetField("TranslationKey", BindingFlags.Static | BindingFlags.Public);
			if (fi == null) return type.Name;
			return (string)fi.GetValue(null);
		}
	}
}
