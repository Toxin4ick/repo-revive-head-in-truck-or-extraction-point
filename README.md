# ReviveHeadInTruckOrExtractionPoint

When a player dies, their can be brought back to life if placed in a safe zone (Truck or Extraction Point).

---

## 🌟 Key Features

* **Safe-Zone Revivals:** Dead players can be revived if their head is located inside the **Truck** or at an **Extraction Point**.
* **Cart Safety Check:** Prevents accidental or bugged revivals while the head is inside a cart.
* **Balanced Scaling Cost:** Revival costs scale dynamically based on current level and the number of times a player has died during the run.
* **Voice Chat Integration:** The head announces the revival cost and countdown via chat.

---

## ⚙️ Configuration Options

The mod generates a configuration file at `BepInEx/config/HT.ReviveHeadInTruckOrExtractionPoint.cfg` with the following parameters:

| Parameter | Type | Default | Range | Description |
| :--- | :--- | :--- | :--- | :--- |
| **`ReviveCooldownSeconds`** | `int` | `300` | `1 – 600` | Cooldown period (in seconds) that a player must wait after being revived before they can be revived again. |
| **`InstaRevive`** | `bool` | `false` | `true / false` | If set to `true`, players skip the chat countdown and revive immediately upon meeting cost and location checks. |
| **`ReviveCostMultiplier`** | `float` | `1.0` | `0.0 – 10.0` | Global multiplier for the revival cost formula (set lower for casual gameplay, higher for hard difficulty). |
| **`HealMultiplier`** | `float` | `0.5` | `0.1 – 1.0` | Percentage of max health restored when a player is revived (e.g., `0.5` restores 50% max HP). |

---
