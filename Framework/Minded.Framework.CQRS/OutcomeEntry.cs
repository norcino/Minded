﻿namespace Minded.Framework.CQRS.Abstractions
{
    /// <inheritdoc cref="IOutcomeEntry"/>
    public class OutcomeEntry : IOutcomeEntry
    {
        /// <summary>
        /// Creates a new ValidationEntry.
        /// </summary>
        public OutcomeEntry(string propertyName, string error) : this(propertyName, error, null)
        {
        }

        /// <summary>
        /// Creates a new ValidationEntry.
        /// </summary>
        public OutcomeEntry(string propertyName, string error, object attemptedValue)
        {
            PropertyName = propertyName;
            Message = error;
            AttemptedValue = attemptedValue;
        }

        /// <inheritdoc cref="IOutcomeEntry.PropertyName"/>
        public string PropertyName { get; private set; }

        /// <inheritdoc cref="IOutcomeEntry.Message"/>
        public string Message { get; private set; }

        /// <inheritdoc cref="IOutcomeEntry.AttemptedValue"/>
        public object AttemptedValue { get; private set; }

        /// <inheritdoc cref="IOutcomeEntry.Severity"/>
        public Severity Severity { get; set; }

        /// <inheritdoc cref="IOutcomeEntry.ErrorCode"/>
        public string ErrorCode { get; set; }

        /// <inheritdoc cref="IOutcomeEntry.ResourceName"/>
        public string ResourceName { get; set; }

        /// <inheritdoc cref="IOutcomeEntry.ToString"/>
        public override string ToString()
        {
            return Message;
        }
    }
}