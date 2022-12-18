using System;
using System.Diagnostics;
using System.Text.Json.Serialization;
using TetraPak.XP.DynamicEntities;
using TetraPak.XP.Serialization;


namespace DCAF.Model
{
    [JsonConverter(typeof(DynamicEntityJsonConverter<Member>))]
    [JsonKeyFormat(KeyTransformationFormat.None)]
    [DebuggerDisplay("{ToString()}")]
    public class Member : ModifiableEntity
    {
        public const string MissingId = "?";
        
        /// <summary>
        ///   Gets/sets the member's internal (Discord) id.
        /// </summary>
        public string Id
        {
            get => Get<string>()!; 
            set => Set(value);
        }

        /// <summary>
        ///   Gets/sets the member's date/time of application (in Zulu time)
        /// </summary>
        public DateTime DateOfApplication 
        {
            get => Get<DateTime>().ToUniversalTime()!; 
            set => Set(value, true);
        }
        
        /// <summary>
        ///   Gets a value indicating whether the member has a working (Discord) id.
        /// </summary>
        public bool IsIdentifiable => Id != MissingId;

        /// <summary>
        ///   Gets/sets the member's forename.
        /// </summary>
        public string Forename
        {
            get => Get<string>()!; 
            set => Set(value, true);
        }

        /// <summary>
        ///   Gets/sets the member's surname.
        /// </summary>
        public string? Surname
        {
            get => Get<string>()!; 
            set => Set(value, true);
        }

        /// <summary>
        ///   Gets/sets the member's personal callsign, if available.
        /// </summary>
        public string? Callsign
        {
            get => Get<string?>(); 
            set => Set(value, true);
        }
        
        /// <summary>
        ///   Gets/sets the member's email address.
        /// </summary>
        public string? Email
        {
            get => Get<string?>(); 
            set => Set(value, true);
        }

        /// <summary>
        ///   Gets/sets the member's current grade (<see cref="MemberGrade"/>).
        /// </summary>
        public MemberGrade Grade 
        {
            get => Get<MemberGrade>(); 
            set => Set(value, true);
        }

        public override string ToString() => ToString(false);

        public string ToString(bool includeId)
        {
            var callsign = string.IsNullOrWhiteSpace(Callsign) ? string.Empty : $"'{Callsign}' ";
            var forename = string.IsNullOrWhiteSpace(callsign)
                ? string.IsNullOrWhiteSpace(Forename) ? string.Empty : $"{Forename[0]}."
                : Forename;
            var surname = string.IsNullOrWhiteSpace(Surname) ? string.Empty : Surname;
            var id = includeId
                ? $" ({Id})"
                : string.Empty; 
            return $"{forename} {callsign}{surname}{id}";
        }

        public MemberStatus Status
        {
            get => Get<MemberStatus>();
            set => Set(value, true);
        }

        public Member(
            string id, 
            DateTime dateOfApplication, 
            string forename, 
            string? surname, 
            MemberGrade grade, 
            MemberStatus status)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            DateOfApplication = dateOfApplication;
            Forename = forename;
            Surname = surname;
            Grade = grade;
            Status = status;
        }
    }
}