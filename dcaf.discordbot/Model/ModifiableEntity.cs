using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using TetraPak.XP;
using TetraPak.XP.DynamicEntities;
using TetraPak.XP.Serialization;

namespace DCAF.DiscordBot.Model
{
    [JsonKeyFormat(KeyTransformationFormat.CamelCase)]
    public class ModifiableEntity : DynamicEntity
    {
        readonly HashSet<string> _updatedKeys = new();

        public bool IsModified => _updatedKeys.Any();

        public bool IsOperational { get; private set; }

        public void ResetModified() => _updatedKeys.Clear();

        public void SetOperational() => IsOperational = true;

        public string[] GetUpdatedProperties() => _updatedKeys.ToArray();

        public override void Set<TValue>(TValue value, string? caller = null) => Set(value, true, caller);

        protected void Set(object? value, bool setIsModified = true, [CallerMemberName] string? propertyName = null)
        {
            if (!TryGetValue(propertyName!, out var existing))
            {
                Add(propertyName!, value);
                if (setIsModified)
                {
                    setModified(propertyName!);
                }
                return;
            }
            
            if (existing!.Equals(value))
                return;

            this[propertyName!] = value;
            setModified(propertyName!);
        }

        void setModified(string propertyName)
        {
            if (IsOperational && !_updatedKeys.Contains(propertyName))
            {
                _updatedKeys.Add(propertyName!);
            }
        }
    }
}