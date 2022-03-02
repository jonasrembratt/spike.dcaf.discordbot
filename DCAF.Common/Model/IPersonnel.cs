using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TetraPak.XP;

namespace DCAF.Model
{
    public interface IPersonnel<T> : IEnumerable<T> where T : Member
    {
        event EventHandler Ready;

        Task<Outcome<IEnumerable<T>>> GetAllMembers();

        Task<Outcome<T>> GetMemberWithIdAsync(string id);

        Task<Outcome<T>> GetMemberWithEmailAsync(string email);
        
        Task<Outcome> UpdateAsync(params T[] members);

        Task<Outcome> ResetAsync();
    }
}