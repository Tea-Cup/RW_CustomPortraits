using UnityEngine;
using Verse;

namespace Foxy.CustomPortraits {
	public class ConstantPortraitCondition : PortraitCondition {
		public const string TranslationKey = "Foxy.CustomPortraits.Condition.Constant";

		public ConstantPortraitCondition() { }
		public ConstantPortraitCondition(PortraitPosition? position, string filename) {
			this.position = position;
			this.filename = filename;
		}
		public override void Draw(Pawn p, Rect left, Rect center, Rect right) {
			base.Draw(p, left, center, right);
		}

		public override bool CheckFor(Pawn p) {
			return filename != null;
		}
	}
}
