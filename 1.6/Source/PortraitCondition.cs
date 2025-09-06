using UnityEngine;
using Verse;

namespace Foxy.CustomPortraits {
	public abstract class PortraitCondition : IExposable {
		public PortraitPosition? position = null;
		public string filename = null;

		private static readonly Color borderEmpty = new Color(0.5f, 0.5f, 0.5f);
		private static readonly Color borderError = new Color(0.7f, 0.3f, 0.3f);
		private static readonly Color hover = new Color(1, 1, 1, 0.3f);

		public abstract bool CheckFor(Pawn p);
		public virtual void Draw(Pawn p, Rect left, Rect center, Rect right) {
			Texture2D tex = PortraitCache.Get(filename);
			if (tex != null) {
				GUI.DrawTexture(right, tex);
			} else {
				GUI.color = string.IsNullOrEmpty(filename) ? borderEmpty : borderError;
				Widgets.DrawBox(right, filename == null ? 1 : 2);
			}
			if (Mouse.IsOver(right)) {
				GUI.color = hover;
				GUI.DrawTexture(right, BaseContent.WhiteTex);
			}
			GUI.color = Color.white;
			if (Widgets.ButtonInvisible(right, true)) {
				GUI_FileDialog.QuickSelect(
					Helper.Label("PortraitFor", p),
					filename,
					(s) => {
						filename = s;
						Log.Message($"new texture for {p.NameFullColored}: \"{filename}\"");
						p.GetPortraits().Update(p);
					}
				);
			}
		}

		public virtual string GetTexture(Pawn p) {
			return filename;
		}

		public virtual void ExposeData() {
			Scribe_Values.Look(ref position, "position", null);
			Scribe_Values.Look(ref filename, "filename", null);
		}

		public override string ToString() {
			return $"{{{GetType().Name} {position?.ToString() ?? "<null>"} \"{filename.ToStringSafe()}\"}}";
		}
	}
}
