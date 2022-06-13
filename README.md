# NullspaceBosses-Samples

Small excerpt from the codebase of Nullspace: Bosses (working title). Keep in mind that this is a solo project and pre-alpha.

**Abilities**: Abstract and specific classes that provide the logic and structure for the actual abilities that are loaded in from a sheet.

**NPCs/AI:**  A simple utility-based AI that weighs potential actions to take based on the game state and each action's considerations.

**Trackers:** 3 out of several trackers that deal with buffs/debuffs, cooldowns, and the character's resources. They are only accessed by the CharacterStatusAPI, which acts as the interface between each entity and the world.

