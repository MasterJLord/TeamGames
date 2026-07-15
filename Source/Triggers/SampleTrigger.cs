using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.TeamGames.Triggers;

[CustomEntity("TeamGames/SampleTrigger")]
public class SampleTrigger : Trigger {
    public SampleTrigger(EntityData data, Vector2 offset) : base(data, offset) {
        // TODO: read properties from data
    }
}
