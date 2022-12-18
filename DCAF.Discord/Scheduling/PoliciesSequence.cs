using TetraPak.XP.StringValues;

namespace DCAF.Discord.Scheduling
{
    public class PoliciesSequence : MultiStringValue
    {
        public new static PoliciesSequence Empty => new();
        
        public PoliciesSequence(string stringValue) 
        : base(stringValue, "=>")
        {
        }

        PoliciesSequence()
        {
        }
    }
}