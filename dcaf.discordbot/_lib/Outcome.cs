using System;
using System.Diagnostics;

namespace DCAF.DiscordBot._lib
{
    /// <summary>
    ///   A base class for a boolean value that can also carry a message and an exception. 
    /// </summary>
    [DebuggerDisplay("{" + nameof(ToString) + "()}")]
    public class Outcome
    {
        readonly string? _message;
        
        /// <summary>
        ///   Gets the value used when objects of this class is cast to a <see cref="bool"/> value.
        /// </summary>
        protected bool Evaluated { get; }

        /// <summary>
        ///   A message to be carried by the <see cref="Outcome"/> object
        ///   (useful for error handling).
        /// </summary>
        public string Message => _message ?? Exception!.Message; 

        /// <summary>
        ///   An <see cref="Exception"/> to be carried by the <see cref="Outcome"/> object
        ///   (useful for error handling).
        /// </summary>
        public Exception? Exception { get; }

        public bool HasMessage => !string.IsNullOrWhiteSpace(Message);

        /// <summary>
        ///   Implicitly casts the <see cref="bool"/> to a <see cref="Outcome"/> value.
        /// </summary>
        public static implicit operator bool(Outcome self) => self.Evaluated;

        /// <summary>
        ///   Constructs and returns an <see cref="Outcome"/> that equals <c>true</c>
        ///   when cast to a <see cref="bool"/> value.
        /// </summary>
        /// <returns>
        ///   A <see cref="Outcome{T}"/> that represents a <c>true</c> value when
        ///   cast as a <see cref="bool"/> while also carrying a specified value.
        /// </returns>
        public static Outcome Success(string? message = null) => new(true, message, null!);
        
        /// <summary>
        ///   Creates and returns an <see cref="Outcome"/> that equals <c>false</c> when cast to a
        ///   <see cref="bool"/> value.
        /// </summary>
        /// <param name="message">
        ///   A message to be carried by the <see cref="Outcome"/> object
        ///   (useful for error handling).
        /// </param>
        /// <param name="exception">
        ///   An <see cref="Exception"/> to be carried by the <see cref="Outcome"/> object
        ///   (for error handling).
        /// </param>
        /// <returns>
        ///   A <see cref="Outcome"/> that represents a <c>true</c> value when
        ///   cast as a <see cref="Boolean"/> while also carrying a specified value.
        /// </returns>
        /// <seealso cref="Fail(System.Exception)"/>
        public static Outcome Fail(string message, Exception exception) => new(false, message, exception);

        /// <summary>
        ///   Creates and returns an <see cref="Outcome"/> that equals <c>false</c> when cast to a
        ///   <see cref="bool"/> value.
        /// </summary>
        /// <param name="exception">
        ///   An <see cref="Exception"/> to be carried by the <see cref="Outcome"/> object
        ///   (for error handling).
        /// </param>
        /// <returns>
        ///   A <see cref="Outcome"/> that represents a <c>true</c> value when
        ///   cast as a <see cref="Boolean"/> while also carrying a specified value.
        /// </returns>
        public static Outcome Fail(Exception exception)
        {
            return new Outcome(false, null!, exception);
        }

        /// <summary>
        ///   Overrides base implementation to reflect evaluated state ("success" / "fail").
        /// </summary>
        public override string ToString()
        {
            return Evaluated ? "success" : "fail";
        }

        /// <summary>
        ///   Initializes a <see cref="Outcome"/>.
        /// </summary>
        /// <param name="evaluated">
        ///   Initializes the <see cref="Evaluated"/> property.
        /// </param>
        /// <param name="message">
        ///   Initializes the <see cref="Message"/> property.
        /// </param>
        /// <param name="exception">
        ///   Initializes the <see cref="Outcome"/> property.
        /// </param>
        protected Outcome(bool evaluated, string? message, Exception exception)
        {
            Evaluated = evaluated;
            _message = message;
            Exception = exception;
        }
    }
}