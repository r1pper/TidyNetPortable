using System.Collections.Generic;

namespace Tidy.Core
{
    /// <summary>
    ///     Collection of TidyMessages
    /// </summary>
    public class TidyMessageCollection : List<TidyMessage>
    {
        private int _errors;
        private int _warnings;

        /// <summary>
        ///     Errors - the number of errors that occurred in the most
        ///     recent parse operation
        /// </summary>
        public virtual int Errors
        {
            get { return _errors; }
        }

        /// <summary>
        ///     Warnings - the number of warnings that occurred in the most
        ///     recent parse operation
        /// </summary>
        public virtual int Warnings
        {
            get { return _warnings; }
        }

        /// <summary>
        ///     Adds a message.
        /// </summary>
        /// <param name="message">The message to add.</param>
        public new void Add(TidyMessage message)
        {
            if (message.Level == MessageLevel.Error)
            {
                _errors++;
            }
            else if (message.Level == MessageLevel.Warning)
            {
                _warnings++;
            }

            base.Add(message);
        }
    }
}