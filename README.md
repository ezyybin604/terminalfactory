# terminalfactory

## this is a factory game where you build a factory

it plays in terminal

100% using c# and no packages

things that happen:

- protect yourself from a hungry dragon (it eats people) by feeding it
- build a factory
- maybe have fun

warning: slightly exessive switch statments \*__*potentally*__

## How to play

### Game

Use WASD to move the cursor, I to open the inventory, and K to place a block. O to place selection. (p to pause and Z to select in pause menu)

### Inventory

Once in the inventory, press R/F to move cursor, W to use item, S to exit, H to delete item, and A to enter crafting menu.

### Actually, I give up giving the controls slowly and carefully. Here's all of them! (all of the controls n stuff)

#### Actually i couldnt be bothered to update this but it works for a little bit

1. Game
    - Use WASD to move
    - Press P to pause
    - Press K to break/collect
    - Press O to place
    - Press I to open inventory/view machine progress
    - Press L to view tile contents/view recipe
    - Press J to exhange contents with tile
    - Press J to change machine recipe

2. Pause
    - Use WS to change selection
    - Press Z to select
3. Inventory
    - Use WS to change selection
    - Press Z to select (use item)
    - Press A to enter crafts menu
    - Press X to go back
    - Press H to delete item
    - If your inventory is empty, you will exit.
    - When an item is deleted, selection will be too.
4. Crafting Menu
    - Use WS to change selection",
    - Press Z to select",
    - Press X to go back"


### Felt like including how to make machines on the readme so here you go
```

M = the machine tile (determines type of machine)
+,-,| = machine blocks (filler, but necssary on corners, also later im gonna add a bonus for higher tiers)
* = energy port
<>^v = Inputs/Outputs, you can determine by hovering over them or looking weather they face or don't at the machine tile.
@ = world interactor (useful for machines like miners/pumps)


3x3 structure :) (example, basic structure)
+-+
|M|
+-+

Example machines

# Assembler
+v+
*A>
+^+

# Coal Generator
+v+
|C>
+-+

# Miner (c = coal ore)
 +*+
c@C>
 +-+

```

### Endgame
```
There are no "endings".
-The factory must grow.
Let the smoke pollute the air-
Oh wait, wrong game
But do grow your factory though.
And since this game basically goes on however long you feel like,
There's a button to save your progress after all.

But basically the end of the game is you shoot a giant high-tech missile at the dragon.
(This isn't in the game yet)
```
