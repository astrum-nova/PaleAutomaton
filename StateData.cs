using System.Collections.Generic;

namespace PaleAutomaton;

public static class StateData
{
    private static readonly HashSet<string> parryableStates =
    [
        "Dive",
        "DashStab Dash",
        "Stab 1",
        "Stab 2",
        "Stab 3",
        "Stab 4",
        "Stab End",
        "Stab End 2",
        "Dash Slash 1",
        "Dash Slash 2",
        "Dash Slash End",
        "Rising Slash",
    ];
    public static bool IsInParryableState() => parryableStates.Contains(PaleAutomatonPlugin.controlFsm.ActiveStateName);
}