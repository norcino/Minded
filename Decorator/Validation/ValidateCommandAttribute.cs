using System;

namespace Minded.Decorator.Validation
{
    public class ValidateCommandAttribute : Attribute
    {
        public bool Validate { get; set; }

        public ValidateCommandAttribute()
        {
            Validate = true;
        }
    }
}
