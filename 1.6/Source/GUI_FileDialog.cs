using System;
using System.IO;
using UnityEngine;
using Verse;

namespace Foxy.CustomPortraits {
	public class GUI_FileDialog {
		private Vector2 scrollPosition = Vector2.zero;
		public DirectoryInfo CurrentDirectory { get; set; } = PortraitCache.Directory;
		public string SelectedPath { get; set; } = null;

		private static readonly float itemHeight = 24;
		private static GUIStyle DirItemContainerStyle { get; } = new GUIStyle() {
			fixedHeight = itemHeight,
			normal = new GUIStyleState() { background = null },
			hover = new GUIStyleState() { background = TexUI.TextBGBlack }
		};
		private static GUIStyle DirItemSelectedContainerStyle { get; } = new GUIStyle() {
			fixedHeight = itemHeight,
			normal = new GUIStyleState() { background = TexUI.GrayBg },
			hover = new GUIStyleState() { background = TexUI.GrayTextBG }
		};
		private static GUIStyle DirItemIconStyle { get; } = new GUIStyle() {
			fixedHeight = itemHeight,
			fixedWidth = itemHeight - 4,
			margin = new RectOffset(0, 0, 4, 0),
			normal = new GUIStyleState() { background = null, textColor = Color.white }
		};
		private static GUIStyle DirItemTextStyle { get; } = new GUIStyle(Text.CurFontStyle) {
			fixedHeight = itemHeight,
			normal = new GUIStyleState() { background = null, textColor = Color.white },
			alignment = TextAnchor.MiddleLeft,
			wordWrap = false
		};

		public string Draw(Rect rect) {
			string clicked = null;
			try {
				GUILayout.BeginArea(rect);
				scrollPosition = GUILayout.BeginScrollView(scrollPosition);
				GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
				clicked = DrawDir(CurrentDirectory);
				if (clicked != null) {
					SelectedPath = clicked;
				} else {
					clicked = SelectedPath;
				}
				GUILayout.EndVertical();
				GUILayout.EndScrollView();
				GUILayout.EndArea();
			} catch (ArgumentException) {
				// Getting control 0's position in a group with only 0 controls when doing repaint
				// Aborting
			}
			return clicked;
		}

		private static bool DrawDirItem(Texture2D icon, string text, bool selected = false) {
			GUILayout.BeginHorizontal(selected ? DirItemSelectedContainerStyle : DirItemContainerStyle, GUILayout.ExpandWidth(true));
			GUILayout.Box(icon ?? Static.texBlank, DirItemIconStyle);
			bool cText = GUILayout.Button(text, DirItemTextStyle, GUILayout.ExpandWidth(true));
			GUILayout.EndHorizontal();
			return cText;
		}
		private string DrawDir(DirectoryInfo dir) {
			if (!dir.FullName.EqualsIgnoreCase(PortraitCache.Directory.FullName)) {
				if (DrawDirItem(Static.texFolderUp, "..")) {
					CurrentDirectory = CurrentDirectory.Parent;
				}
			}
			foreach (DirectoryInfo di in dir.EnumerateDirectories()) {
				if (DrawDirItem(Static.texFolder, di.Name)) {
					CurrentDirectory = di;
				}
			}
			string clicked = null;
			foreach (FileInfo fi in dir.EnumerateFiles()) {
				string filename = PortraitCache.GetRelativePath(fi);
				Texture2D portrait = PortraitCache.Get(filename);
				GUI.enabled = portrait != null;
				if (DrawDirItem(portrait, fi.Name, SelectedPath == filename)) {
					clicked = filename;
				}
				GUI.enabled = true;
			}
			return clicked;
		}

		public static void QuickSelect(string title, string initial, Action<string> callback) {
			QuickWindow w = new QuickWindow(title, initial, callback);
			Find.WindowStack.Add(w);
		}

		private class QuickWindow : Window {
			private readonly GUI_FileDialog file_dialog = new GUI_FileDialog();
			private readonly Action<string> callback;
			public override Vector2 InitialSize => new Vector2(310f, 375f);
			protected override float Margin => 5f;

			public QuickWindow(string title, string selected, Action<string> callback) {
				this.callback = callback;
				file_dialog.SelectedPath = selected;
				optionalTitle = title;
				if (!string.IsNullOrEmpty(selected)) {
					file_dialog.CurrentDirectory = new FileInfo(Path.Combine(PortraitCache.Directory.FullName, selected)).Directory;
				}
				PortraitCache.Update();
			}

			public override void DoWindowContents(Rect inRect) {
				inRect.SplitHorizontally(inRect.height - 25, out Rect top, out Rect bottom);

				file_dialog.Draw(top);

				float w = inRect.width / 3;
				Rect left = new Rect(w + 5, bottom.y + 5, w - 10, 20);
				Rect right = new Rect(w + w + 10, bottom.y + 5, w - 10, 20);
				if (Widgets.ButtonText(left, Helper.Label("OK"))) {
					Close();
				}
				if (Widgets.ButtonText(right, Helper.Label("Cancel"))) {
					file_dialog.SelectedPath = null;
					Close();
				}

				if (Mouse.IsOver(right)) {
					GUI.color = new Color(1, 1, 1, 0.5f);
					GUI.DrawTexture(right, BaseContent.BlackTex);
					GUI.color = Color.white;
				}
			}

			public override void PostClose() {
				if (file_dialog.SelectedPath == null) return;
				callback?.Invoke(file_dialog.SelectedPath);
			}
		}
	}
}
