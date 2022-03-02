namespace DCAF.Discord.Policies
{
    public class PolicyResult
    {
        public string Message { get; set; }

        public PolicyResult()
        {
            
        }
        
        public PolicyResult(string message)
        {
            Message = message;
        }
    }
}