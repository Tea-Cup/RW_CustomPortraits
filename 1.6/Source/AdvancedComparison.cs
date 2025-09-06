using System;
using UnityEngine;

namespace Foxy.CustomPortraits {
	[Flags]
	public enum ComparisonFlags {
		NotEquals = 0b0001,
		LessThan = 0b0010,
		GreaterThan = 0b0110,
		Percents = 0b1000
	}
	public static class AdvancedComparison {
		public const ComparisonFlags EQUALS = 0;
		public const ComparisonFlags NOT_EQUALS = ComparisonFlags.NotEquals;
		public const ComparisonFlags LESS_EQUALS = ComparisonFlags.LessThan;
		public const ComparisonFlags LESS_THAN = ComparisonFlags.LessThan | ComparisonFlags.NotEquals;
		public const ComparisonFlags GREATER_EQUALS = ComparisonFlags.GreaterThan;
		public const ComparisonFlags GREATER_THAN = ComparisonFlags.GreaterThan | ComparisonFlags.NotEquals;

		public const ComparisonFlags PERCENT_EQUALS = ComparisonFlags.Percents | EQUALS;
		public const ComparisonFlags PERCENT_NOT_EQUALS = ComparisonFlags.Percents | NOT_EQUALS;
		public const ComparisonFlags PERCENT_LESS_EQUALS = ComparisonFlags.Percents | LESS_EQUALS;
		public const ComparisonFlags PERCENT_LESS_THAN = ComparisonFlags.Percents | LESS_THAN;
		public const ComparisonFlags PERCENT_GREATER_EQUALS = ComparisonFlags.Percents | GREATER_EQUALS;
		public const ComparisonFlags PERCENT_GREATER_THAN = ComparisonFlags.Percents | GREATER_THAN;

		public static bool Apply(float target, float current, float max, ComparisonFlags flags) {
			float a = current;
			float b = target;
			if(flags.HasFlag(ComparisonFlags.Percents)) {
				a = current / max;
				b = target / 100f;
			}
			
			if (!flags.HasFlag(ComparisonFlags.NotEquals) && Mathf.Approximately(a, b)) return true;
			if (flags.HasFlag(ComparisonFlags.LessThan) && a < b) return true;
			if (flags.HasFlag(ComparisonFlags.GreaterThan) && a < b) return true;
			return false;
		}
	}
}
