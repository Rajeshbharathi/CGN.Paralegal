using System;

namespace LexisNexis.Evolution.Overdrive
{
    [Serializable]
    public class RoleType
    {
        public RoleType(string moniker)
        {
            Moniker = moniker;
        }

        public string Moniker { get; private set; }

        public override string ToString()
        {
            return Moniker;
        }

        public override bool Equals(object obj)
        {
            // Check for null values and compare run-time types.
            if (obj == null)
            {
                return false;
            }

            var other = obj as RoleType;
            if (other == null)
            {
                return false;
            }

            if (0 == String.Compare(Moniker, other.Moniker, StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }
            return false;
        }

        public bool Equals(RoleType other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return (0 == String.Compare(Moniker, other.Moniker, StringComparison.InvariantCultureIgnoreCase));
        }

        public override int GetHashCode()
        {
            return (Moniker != null ? Moniker.GetHashCode() : 0);
        }

        public static bool operator == (RoleType a, RoleType b)
        {
            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(a, b))
            {
                return true;
            }

            // If one is null, but not both, return false.
            if (((object)a == null) || ((object)b == null))
            {
                return false;
            }

            // Return true if the fields match:
            return (0 == String.Compare(a.Moniker, b.Moniker, StringComparison.InvariantCultureIgnoreCase));
        }

        public static bool operator !=(RoleType first, RoleType second)
        {
            return !(first == second);
        }
    }
}