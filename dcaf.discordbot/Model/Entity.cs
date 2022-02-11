using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace DCAF.DiscordBot.Model
{
    public class Entity : Dictionary<string,object?>
    {
        readonly HashSet<string> _updatedKeys = new();

        public bool IsModified => _updatedKeys.Any();

        public bool IsOperational { get; private set; }

        public void ResetModified() => _updatedKeys.Clear();

        public void SetOperational() => IsOperational = true;

        public string[] GetUpdatedProperties() => _updatedKeys.ToArray();
        
        protected T? Get<T>(T? useDefault = default, [CallerMemberName] string? propertyName = null) =>
            TryGetValue(propertyName!, out var obj) && obj is T tv 
                ? tv 
                : useDefault;
        
        protected void Set(object? value, bool setIsModified = false, [CallerMemberName] string? propertyName = null)
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