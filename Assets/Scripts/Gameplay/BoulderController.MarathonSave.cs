using System.Collections.Generic;

public sealed partial class BoulderController
{
    public MarathonMatchSnapshot.BoulderState CaptureMarathonBoulders()
    {
        var state = new MarathonMatchSnapshot.BoulderState { countdown = countdown, elapsed = modifierElapsed, pending = bossVolleyPending };
        foreach (var rock in rocks) state.rocks.Add(new MarathonMatchSnapshot.RockState {
            position = rock.position, velocity = rock.velocity, bounces = rock.bounces,
            contacts = new List<UnityEngine.Vector2Int>(rock.contacts) });
        return state;
    }
    public void RestoreMarathonBoulders(MarathonMatchSnapshot.BoulderState state)
    {
        Clear();
        board = gc.gameBoard;
        countdown = state.countdown; modifierElapsed = state.elapsed; bossVolleyPending = state.pending;
        foreach (var saved in state.rocks)
        {
            Spawn(saved.position.x);
            var rock = rocks[rocks.Count - 1];
            rock.position = saved.position; rock.velocity = saved.velocity; rock.bounces = saved.bounces;
            rock.contacts.UnionWith(saved.contacts);
            rock.image.rectTransform.anchoredPosition = ToLocal(rock.position);
        }
    }
}
