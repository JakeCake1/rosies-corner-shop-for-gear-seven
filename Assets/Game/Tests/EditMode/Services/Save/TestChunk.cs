using Game.Services.Save;
using Newtonsoft.Json.Linq;

namespace Game.Tests.EditMode.Services.Save
{
    /// <summary>
    /// Version 2 chunk for save tests: version 1 stored the name as "title", version 2 calls it "name".
    /// </summary>
    internal sealed class TestChunk : ProfileChunk<TestChunkPayload>
    {
        public override string Key => "test";

        public override int Version => 2;

        public string Name => Data.Name;

        public int Count => Data.Count;

        public void Set(string name, int count)
        {
            Data.Name = name;
            Data.Count = count;
            MarkChanged();
        }

        public override void Migrate(JObject data, int fromVersion)
        {
            if (fromVersion >= 2 || data["title"] == null)
            {
                return;
            }

            data["name"] = data["title"];
            data.Remove("title");
        }
    }
}
