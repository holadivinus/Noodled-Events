# All Custom Nodes

<details>
<summary>flow.if</summary>
  Lets you do stuff conditionally; <br>
  If the condition is true It'll run the "true" pin.<br>
  Otherwise, the "false" pin.<br>
  
![picture of flow if node](Docs/flow.if.png)
</details>

<details>
<summary>Math Operators</summary>
math.add, math.sub, math.mul & math.div manipulate numbers.<br>
They take two numbers (a, b) and add, subtruct, multiply or divide them.<br>

math.greater & math.lesser takes two numbers, compare them, and returns a bool (True/False) for use with the flow.if node.<br>
![picture of plus minus multiply divide greater lesser math nodes](Docs/math.png)
</details>

<details>
<summary>Variables</summary>
  There are many Variable Nodes availiable, each prefixed by "vars." for easy searching.<br>
  Vars can store:<br>
- floats (aka decimal numbers like 3.14, .3333, -7.89), <br>
- ints (aka whole numbers like 5, 10, 300, -2), <br>
- strings (aka text & words), <br>
- Vector3s (aka Coordinates, as in X Y Z), <br>
- bools (true/false),<br>
- UnityEngine.Objects (This can be anything worldly! Like a nullbody, a gun, the player, a zone, etc...)<br>
  There are 2 ways Variables are stored: gobj vars, and scene vars.<br>
  <br>
  <em><strong>Scene vars</strong></em> are publicly accessible by name, and can be used by any ult-event anywhere.<br>
  Events from spawnables, levels, and avatars can all communicate via scene vars.<br>
  (Ex: a magic stopwatch that edits the "TimeOfDay" Scene variable, messing with a Day Night cycle world.)<br>
  <br>
  <em><strong>Gobj vars</strong></em> are private; You have to supply a Gameobject this variable will latch on to, meaning<br>
  no other mod could mess with it without some extreme ult-event tomfoolery.<br>
  <br>
  <br>
  (the get_or_init variant of vars allow you to specify a default value.) <br>
  
  ![picture a few var nodes](Docs/vars.png)
</details>

<details>
<summary>async.Wait</summary>
  This node will delay execution for the specified time.<br>
  
  ![picture of async.Wait node](Docs/wait.png)
</details>

<details>
<summary>Loops</summary>
loops.for, loops.while, loops.continue and loops.break all let you run events over and over again.<br>
Don't create an infinite loop - it'll freeze unity and bonelab.<br>
    <br>
The base concept of a "For" and "While" Loop won't be explained here.<br>
  <br>
A loop's body must always end in either a loops.continue, or a loops.break node.<br>
This is because "loops.continue" is what's actually responsible for looping,<br>
While "loops.break" will skip to the "Done" pin and immediately end the loop.<br>
    <br>
If you don't hit either, the loop wont continue or end, and instead will just die out immediately.<br>
  
![picture of for loop](Docs/forloop.png)<br>
(fyi loops.continue and loops.break are compatible with async.wait)<br>
</details>
