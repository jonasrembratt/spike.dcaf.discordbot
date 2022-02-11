using System;
using System.Diagnostics;

namespace DCAF.DiscordBot._lib
{
    /// <summary>
    ///   A boolean value that can also carry another value. This is
    ///   very useful as a typical return value where you need an indication
    ///   for "success" and, when successful, a value.
    /// </summary>
    /// <typeparam name="T">
    ///   The type of value carried by the <see cref="Outcome{T}"/>.
    /// </typeparam>
    /// <remarks>
    ///   Instances of type <see cref="Outcome{T}"/> can be implicitly cast to
    ///   a <c>bool</c> value. Very useful for testing purposes.
    /// </remarks>
    [DebuggerDisplay("{" + nameof(ToString) + "()}")]
    public class Outcome<T> : Outcome
    {
        /// <summary>
        ///   The value carried by the object.
        /// </summary>
        public T? Value { get; private set; }

        /// <summary>
        ///   Creates a <see cref="Outcome{T}"/> that equals <c>true</c> when cast to a
        ///   <see cref="bool"/> value.
        /// </summary>
        /// <param name="value">
        ///   The value (of type <typeparamref name="T"/>) to be carried by the
        ///   new <see cref="Outcome{T}"/> object.
        /// </param>
        /// <returns>
        ///   A <see cref="Outcome{T}"/> that represents a <c>true</c> value when
        ///   cast as a <see cref="bool"/> while also carrying a specified value.
        /// </returns>
        public static Outcome<T> Success(T value) => new(true, null!, null!, value);

        /// <summary>
        ///   Creates a <see cref="Outcome{T}"/> that equals <c>false</c> when cast to a
        ///   <see cref="bool"/> value.
        /// </summary>
        /// <param name="message">
        ///   A message to be carried by the <see cref="Outcome{T}"/> object
        ///   (useful for error handling).
        /// </param>
        /// <param name="exception">
        ///   An <see cref="Exception"/> to be carried by the <see cref="Outcome{T}"/> object
        ///   (for error handling).
        /// </param>
        /// <returns>
        ///   A <see cref="Outcome{T}"/> that represents a <c>true</c> value when
        ///   cast as a <see cref="Boolean"/> while also carrying a specified value.
        /// </returns>
        public new static Outcome<T> Fail(string message, Exception exception) => new(false, message, exception, default!);

        /// <summary>
        ///   Creates and returns a failed outcome that carries an <see cref="Exception"/> as well as a value.
        /// </summary>
        /// <param name="exception">
        ///   Assigns <see cref="Exception"/>.
        /// </param>
        /// <param name="value">
        ///   Assigns <see cref="Value"/>.
        /// </param>
        /// <returns>
        ///   An <see cref="Outcome{T}"/> to indicate failure.
        /// </returns>
        public static Outcome<T> Fail(Exception exception, T value) => new(false, null!, exception, default!);

        /// <summary>
        ///   Creates a <see cref="Outcome{T}"/> that equals <c>false</c> when cast to a
        ///   <see cref="bool"/> value.
        /// </summary>
        /// <param name="exception">
        ///   An <see cref="Exception"/> to be carried by the <see cref="Outcome{T}"/> object
        ///   (for error handling).
        /// </param>
        /// <returns>
        ///   A <see cref="Outcome{T}"/> that represents a <c>true</c> value when
        ///   cast as a <see cref="Boolean"/> while also carrying a specified value.
        /// </returns>
        public new static Outcome<T> Fail(Exception exception) => new(false, null!, exception, default!);
        
        public override string ToString()
        {
            return Evaluated ? $"success : {value()}" : $"fail{errorMessage()}";

            string value() => ReferenceEquals(default, Value) ? "(null)" : Value.ToString();

            string errorMessage()
            {
                if (Exception is null)
                    return "";

                return $" ({Exception.Message})";
            }
        }

        /// <summary>
        ///   Implicitly converts the outcome to the expected value.
        /// </summary>
        /// <param name="outcome">
        ///   The outcome.
        /// </param>
        /// <returns>
        ///   The expected (successful) outcome value.
        /// </returns>
        public static implicit operator T?(Outcome<T?> outcome) => outcome.Value;

        Outcome(bool evaluated, string message, Exception exception, T? value) 
        : base(evaluated, message, exception)
        {
            Value = value;
        }
    }
}