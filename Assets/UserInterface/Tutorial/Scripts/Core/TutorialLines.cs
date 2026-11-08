using System;
using UnityEngine;

namespace ArcaneKeepers.Tutorial
{
    /// <summary>
    /// Everything Dog_Hero says, editable on the TutorialDirector in the Inspector.
    /// &lt;k&gt;word&lt;/k&gt; colours a keyword; {name} becomes the Keeper the player just placed.
    /// Keep lines short: two lines in the bubble reads best on a phone.
    /// </summary>
    [Serializable]
    public class TutorialLines
    {
        [Header("Intro")]
        [TextArea(2, 3)] public string welcome = "Woof! Welcome to the castle, Keeper!";
        [TextArea(2, 3)] public string intro = "I'm captain of the guard. Stick with me and I'll show you the ropes!";

        [Header("Objective")]
        [TextArea(2, 3)] public string enemies = "Monsters march in from the <k>right</k>, one lane at a time...";
        [TextArea(2, 3)] public string castle = "If even one reaches the <k>castle</k> on the left, we lose!";
        [TextArea(2, 3)] public string meter = "This meter tracks the <k>waves</k>. Beat every wave to win!";

        [Header("Cards + placing")]
        [TextArea(2, 3)] public string cards = "These are your <k>Keeper cards</k>, the squad you picked.";
        [TextArea(2, 3)] public string drag = "Drag a Keeper onto the <k>glowing tile</k>!";
        [TextArea(2, 3)] public string dragNudge = "Go on! Press a card and slide it onto the tile.";
        [TextArea(2, 3)] public string dragMissed = "So close! Let go right on top of a tile.";
        [TextArea(2, 3)] public string placed = "Pawsome! <k>{name}</k> is on guard!";
        [TextArea(2, 3)] public string cardRests = "While a Keeper is out, their card <k>rests</k>. If they fall, it <k>recharges</k> first.";
        [TextArea(2, 3)] public string second = "Let's cover another lane. Drag in one more Keeper!";
        [TextArea(2, 3)] public string secondPlaced = "Two lanes guarded. You're a natural!";

        [Header("Moving")]
        [TextArea(2, 3)] public string relocate = "Need to move someone? Tap the <k>move</k> button.";
        [TextArea(2, 3)] public string relocateDrag = "Now drag your Keeper to a <k>new tile</k>!";
        [TextArea(2, 3)] public string relocateDone = "Nice footwork!";
        [TextArea(2, 3)] public string relocateMissed = "Hold on to them and drop them on an <k>empty</k> tile.";
        [TextArea(2, 3)] public string relocateGiveUp = "No worries, you'll get the hang of it. Onward!";

        [Header("Wrap up")]
        [TextArea(2, 3)] public string go = "Here they come! Hold the line, Keeper!";
        [TextArea(2, 3)] public string skipped = "Good luck out there, Keeper!";

        [Header("During the first battle")]
        [TextArea(2, 3)] public string firstKill = "Got one! Woof!";
        [TextArea(2, 3)] public string keeperDown = "Ouch! Their card will recharge, hang in there.";
        [TextArea(2, 3)] public string finalWave = "Final wave! Give it everything!";
    }
}
