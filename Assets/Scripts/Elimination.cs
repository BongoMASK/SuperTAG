using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Elimination : GameMode
{
    public override void OnRoundEnd() {
        // Switch Denner
        // Record Time
    }

    public override void OnTag() {
        // Switch Teams
    }

    public override void OnTimeIsZero() {
        // Kill Denner
    }
}
