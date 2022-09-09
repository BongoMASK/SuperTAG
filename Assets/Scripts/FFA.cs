using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FFA : GameMode {

    public override void OnRoundEnd() {
        // Set scores
        // Increment round count
    }

    public override void OnTag() {
        // Switch Teams
    }

    public override void OnTimeIsZero() {
        // End round
        OnRoundEnd();
    }
}
