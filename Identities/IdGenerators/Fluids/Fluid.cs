using System;

// ReSharper disable once CheckNamespace
namespace Architect.Identities
{
	/// <summary>
	/// <para>
	/// A 64-bit, flexible, locally unique identifier.
	/// </para>
	/// <para>
	/// Within certain parameters, this type alone can replace both a UUID/GUID and an auto-increment ID, allowing a single ID to be used internally and externally.
	/// </para>
	/// <para>
	/// Values created through Create() are practically ascending.
	/// There may be slight deviations where multiple systems have millisecond clock differences and values are created around the same time.
	/// </para>
	/// </summary>
	public readonly struct Fluid : IEquatable<Fluid>, IComparable<Fluid>
	{
		public override string ToString() => this.IntegralValue.ToString();
		public override int GetHashCode() => this.IntegralValue.GetHashCode();
		public override bool Equals(object? obj) => obj is Fluid other && this.Equals(other);
		public bool Equals(Fluid other) => this.IntegralValue.Equals(other.IntegralValue);
		public static bool operator ==(Fluid self, Fluid other) => self.Equals(other);
		public static bool operator !=(Fluid self, Fluid other) => !(self == other);
		public static bool operator ==(Fluid self, ulong other) => self.IntegralValue.Equals(other);
		public static bool operator !=(Fluid self, ulong other) => !(self == other);
		public static bool operator ==(Fluid self, long other) => other >= 0 && self.IntegralValue.Equals((ulong)other);
		public static bool operator !=(Fluid self, long other) => !(self == other);
		public static bool operator ==(Fluid self, int other) => self == (long)other; // Mainly to enable "== 0"
		public static bool operator !=(Fluid self, int other) => !(self == other); // Mainly to enable "!= 0"
		public int CompareTo(Fluid other) => this.IntegralValue.CompareTo(other.IntegralValue);

		private ulong IntegralValue { get; }

		/// <summary>
		/// Returns false if the value is equal to the default of 0, or true if it is greater.
		/// </summary>
		public bool HasValue => this.IntegralValue != default;
		
		// #TODO: Use AmbientScope
		/// <summary>
		/// Returns a new locally unique identifier.
		/// </summary>
		public static Fluid Create()
		{
			return FluidIdGenerator.Default.CreateFluid();
		}

		/// <summary>
		/// Interprets the given integral value as a Fluid.
		/// </summary>
		internal Fluid(ulong integralValue)
		{
			this.IntegralValue = integralValue;
		}

		/// <summary>
		/// Interprets the given integral value as a Fluid.
		/// </summary>
		internal Fluid(long integralValue)
			: this(integralValue >= 0 ? (ulong)integralValue : throw new ArgumentException($"The value must not be negative."))
		{
		}

		public static implicit operator Fluid(ulong ulongValue) => new Fluid(ulongValue);
		public static implicit operator ulong(Fluid fluid) => fluid.IntegralValue;
		public static implicit operator Fluid(long longValue) => new Fluid(longValue);
		public static implicit operator long(Fluid fluid) => fluid.IntegralValue > Int64.MaxValue
			? throw new OverflowException($"{nameof(Fluid)} {fluid} cannot be converted to {nameof(Int64)} because it overflows {nameof(Int64)}.{nameof(Int64.MaxValue)}.")
			: (long)fluid.IntegralValue;
	}
}
