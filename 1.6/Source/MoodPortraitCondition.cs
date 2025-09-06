using UnityEngine;
using Verse;

namespace Foxy.CustomPortraits {
	public class MoodPortraitCondition : PortraitCondition {
		public const string TranslationKey = "Foxy.CustomPortraits.Condition.Mood";
		public int value = 50;
		public ComparisonFlags flags = AdvancedComparison.PERCENT_LESS_THAN;

		public override bool CheckFor(Pawn p) {
			return AdvancedComparison.Apply(value, p.needs.mood.CurLevel, p.needs.mood.MaxLevel, flags);
		}
		public override void Draw(Pawn p, Rect left, Rect center, Rect right) {
			base.Draw(p, left, center, right);
		}

		public override void ExposeData() {
			base.ExposeData();
			Scribe_Values.Look(ref value, "value", 50);
			Scribe_Values.Look(ref flags, "flags", AdvancedComparison.PERCENT_LESS_THAN);
		}
	}
}
