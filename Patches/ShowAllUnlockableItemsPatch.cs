using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace LCModTest.Patches {
    [HarmonyPatch(typeof(Terminal))]
    public class ShowAllUnlockableItemsPatch {
        [HarmonyPatch("RotateShipDecorSelection")]
        [HarmonyPrefix]
        private static bool RotateShipDecorSelectionPrefix(Terminal __instance) {
			// Show all unlockablesList
			Terminal terminalInstance = Object.FindObjectOfType<Terminal>();
			terminalInstance.ShipDecorSelection.Clear();
			for (int i = 0; i < StartOfRound.Instance.unlockablesList.unlockables.Count; i++) {
				if (StartOfRound.Instance.unlockablesList.unlockables[i].shopSelectionNode != null && !StartOfRound.Instance.unlockablesList.unlockables[i].alwaysInStock) {
					terminalInstance.ShipDecorSelection.Add(StartOfRound.Instance.unlockablesList.unlockables[i].shopSelectionNode);
				}
			}

			// Not to execute original function
			return false;
        }
    }
}

