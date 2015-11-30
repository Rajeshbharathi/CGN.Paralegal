using System.Collections.Generic;

namespace CGN.Paralegal.ClientContracts.AppState
{
    public class AppState
    {
        /// <summary>
        /// Gets or sets the org identifier.
        /// </summary>
        /// <value>
        /// The org identifier.
        /// </value>
        public long OrgId { get; set; }

        /// <summary>
        /// Gets or sets the matter identifier.
        /// </summary>
        /// <value>
        /// The matter identifier.
        /// </value>
        public long MatterId { get; set; }

        /// <summary>
        /// Gets or sets the dataset identifier.
        /// </summary>
        /// <value>
        /// The dataset identifier.
        /// </value>
        public long DatasetId { get; set; }

        /// <summary>
        /// Gets or sets the project identifier.
        /// </summary>
        /// <value>
        /// The project identifier.
        /// </value>
        public long ProjectId { get; set; }

        protected bool Equals(AppState other)
        {
            return this.OrgId == other.OrgId && this.MatterId == other.MatterId && this.DatasetId == other.DatasetId && this.ProjectId == other.ProjectId;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            if (obj.GetType() != this.GetType())
            {
                return false;
            }
            return Equals((AppState)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = this.OrgId.GetHashCode();
                hashCode = (hashCode * 397) ^ this.MatterId.GetHashCode();
                hashCode = (hashCode * 397) ^ this.DatasetId.GetHashCode();
                hashCode = (hashCode * 397) ^ this.ProjectId.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(AppState left, AppState right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(AppState left, AppState right)
        {
            return !Equals(left, right);
        }

        /// <summary>
        /// Gets or sets the user grops.
        /// </summary>
        /// <value>
        /// The user grops.
        /// </value>
        public List<string> UserGrops { get; set; }
    }
}