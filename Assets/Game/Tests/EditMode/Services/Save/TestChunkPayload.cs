using Newtonsoft.Json;

namespace Game.Tests.EditMode.Services.Save
{
    /// <summary>
    /// Data of <see cref="TestChunk"/> as it is stored in the save file.
    /// </summary>
    internal sealed class TestChunkPayload
    {
        [JsonProperty("name")] public string Name { get; set; } = "none";

        [JsonProperty("count")] public int Count { get; set; }
    }
}
