using System.Text.Json.Serialization;

namespace Minded.Framework.CQRS.Abstractions
{
    /// <inheritdoc cref="IOutcomeEntry"/>
    public class OutcomeEntry : IOutcomeEntry
    {
        /// <summary>
        /// Creates a new OutcomeEntry.
        /// </summary>
        public OutcomeEntry(string propertyName, string message) : this(propertyName, message, null)
        {
        }

        /// <summary>
        /// Creates a new OutcomeEntry.
        /// </summary>
        [JsonConstructor]
        public OutcomeEntry(string propertyName, string message, object attemptedValue)
        {
            PropertyName = propertyName;
            Message = message;
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

        /// <inheritdoc cref="IOutcomeEntry.UniqueErrorCode"/>
        public string UniqueErrorCode { get; set; }

        /// <inheritdoc cref="IOutcomeEntry.ToString"/>
        public override string ToString()
        {
            return Message;
        }
    }
}
