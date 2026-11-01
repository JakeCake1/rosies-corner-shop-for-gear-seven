using System.Collections.Generic;

namespace Game.Services.Content
{
    /// <summary>
    /// A config asset that can check itself; config tests, <c>OnValidate</c> and the build preprocessor call the
    /// same method.
    /// </summary>
    public interface IValidatableConfig
    {
        /// <summary>
        /// Appends a message for every problem found; an empty list means the asset is valid.
        /// </summary>
        /// <param name="errors">Receives messages that name the asset, the field and the actual value.</param>
        void Validate(List<string> errors);
    }
}
