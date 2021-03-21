using System.Collections;
using System.Linq;
using UnityEngine;
using KModkit;
using System.Text.RegularExpressions;
using System;
using System.Collections.Generic;

public class ValuedKeysScript : MonoBehaviour {

    public KMAudio audio;
    public KMBombInfo bomb;

    public KMSelectable[] buttons;
    public TextMesh[] texts;

    private int[] keyVals = new int[4];
    private int[] keyInc = new int[] { 0, 1, 2, 3 };
    private int[] keyDec = new int[] { 0, 1, 2, 3 };

    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;

    void Awake()
    {
        moduleId = moduleIdCounter++;
        moduleSolved = false;
        foreach (KMSelectable obj in buttons)
        {
            KMSelectable pressed = obj;
            pressed.OnInteract += delegate () { PressButton(pressed); return false; };
        }
    }

    void Start () {
        keyInc = Shuffle(keyInc);
        keyDec = Shuffle(keyDec);
        string[] names = new string[] { "top left", "top right", "bottom left", "bottom right" };
        for (int i = 0; i < 4; i++)
        {
            keyVals[i] = bomb.GetSerialNumberNumbers().First();
            Debug.LogFormat("[Valued Keys #{0}] The {1} key will increment the {2} key's value by 1 and decrement the {3} key's value by 1", moduleId, names[i], names[keyInc[i]], names[keyDec[i]]);
        }
        Debug.LogFormat("[Valued Keys #{0}] All keys' values must be set to {1}", moduleId, bomb.GetSerialNumberNumbers().First());
        int times = UnityEngine.Random.Range(10, 21);
        if (UnityEngine.Random.Range(0, 20) == 0)
            times = 0;
        for (int i = 0; i < times; i++)
        {
            int key = UnityEngine.Random.Range(0, 4);
            keyVals[keyInc[key]] += 1;
            if (keyVals[keyInc[key]] > 9)
                keyVals[keyInc[key]] = 0;
            keyVals[keyDec[key]] -= 1;
            if (keyVals[keyDec[key]] < 0)
                keyVals[keyDec[key]] = 9;
        }
        for (int i = 0; i < 4; i++)
            texts[i].text = keyVals[i].ToString();
    }

    void PressButton(KMSelectable pressed)
    {
        if (moduleSolved != true)
        {
            int index = Array.IndexOf(buttons, pressed);
            pressed.AddInteractionPunch();
            audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, pressed.transform);
            keyVals[keyInc[index]] += 1;
            keyVals[keyDec[index]] -= 1;
            if (keyVals[keyInc[index]] > 9)
                keyVals[keyInc[index]] = 0;
            if (keyVals[keyDec[index]] < 0)
                keyVals[keyDec[index]] = 9;
            texts[keyInc[index]].text = keyVals[keyInc[index]].ToString();
            texts[keyDec[index]].text = keyVals[keyDec[index]].ToString();
            if (keyVals.All(x => x == bomb.GetSerialNumberNumbers().First()))
            {
                moduleSolved = true;
                GetComponent<KMBombModule>().HandlePass();
            }
        }
    }

    int[] Shuffle(int[] nums)
    {
        for (int t = 0; t < nums.Length; t++)
        {
            string tmp = nums[t].ToString();
            int r = UnityEngine.Random.Range(t, nums.Length);
            nums[t] = nums[r];
            nums[r] = int.Parse(tmp);
        }
        return nums;
    }

    //twitch plays
    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} press <p1> (p2)... [Presses the key in the specified position (optionally press multiple keys)] | Valid positions are tl, tr, bl, br, and 1-4 in reading order";
    #pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string command)
    {
        string[] parameters = command.Split(' ');
        if (Regex.IsMatch(parameters[0], @"^\s*press\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            if (parameters.Length >= 2)
            {
                string[] valids = new string[] { "tl", "tr", "bl", "br", "1", "2", "3", "4" };
                for (int i = 1; i < parameters.Length; i++)
                {
                    if (!valids.Contains(parameters[i].ToLower()))
                    {
                        yield return "sendtochaterror!f The specified position '" + parameters[i] + "' is invalid!";
                        yield break;
                    }
                }
                for (int i = 1; i < parameters.Length; i++)
                {
                    buttons[Array.IndexOf(valids, parameters[i].ToLower()) % 4].OnInteract();
                    yield return new WaitForSeconds(0.1f);
                }
            }
            else if (parameters.Length == 1)
            {
                yield return "sendtochaterror Please specify the position(s) of the key(s) you wish to press!";
            }
            yield break;
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        string command;
        int firstDigit = bomb.GetSerialNumberNumbers().First();
        if (keyVals.SequenceEqual(new[] { firstDigit, firstDigit, firstDigit, firstDigit }))
        {
            for (int i = 0; i < keyVals.Length; i++)
            {
                if (keyInc[i] == keyDec[i])
                {
                    
                    yield return ProcessTwitchCommand("press " + (i + 1));
                    yield break;
                }
            }
            int indexToPress = UnityEngine.Random.Range(1, 5);
            command = string.Format("press {0}", string.Join(" ", Enumerable.Range(0, 10).Select(x => indexToPress.ToString()).ToArray()));
            yield return ProcessTwitchCommand(command);
        }
        else
        {
            command = string.Format("press {0}", string.Join(" ", BFS().Select(x => x.ToString()).ToArray()));
            yield return ProcessTwitchCommand(command);
        }
        
    }

    class State : IEquatable<State>
    {
        public State PreviousState { get; private set; }
        public int IndexToPress { get; private set; }

        private int[] _currentNumbers;
        private int[] _goal;
        private int[] _inc;
        private int[] _dec;

        public State(int[] currentNumbers, int[] goal, int[] inc, int[] dec, State previousState = null, int indexToPress = -1)
        {
            _currentNumbers = currentNumbers;
            _goal = goal;
            _inc = inc;
            _dec = dec;
            PreviousState = previousState;
            IndexToPress = indexToPress;
        }

        public bool IsGoal()
        {
            return _currentNumbers.SequenceEqual(_goal);
        }

        public List<State> GetSuccessors()
        {
            List<State> successors = new List<State>();
            for (int i = 0; i < _inc.Length; i++)
            {
                int incIndex = _inc[i];
                int decIndex = _dec[i];
                if (incIndex == decIndex)
                    continue;
                int[] newNumbers = _currentNumbers.ToArray();
                newNumbers[incIndex] = (newNumbers[incIndex] + 1) % 10;
                newNumbers[decIndex] = (newNumbers[decIndex] + 9) % 10;
                successors.Add(new State(newNumbers, _goal, _inc, _dec, this, i + 1));
            }
            return successors;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || !GetType().Equals(obj.GetType()))
                return false;

            return Equals((State)obj);
        }

        public bool Equals(State otherState)
        {
            return _currentNumbers.SequenceEqual(otherState._currentNumbers) &&
                   _goal.SequenceEqual(otherState._goal) &&
                   _inc.SequenceEqual(otherState._inc) &&
                   _dec.SequenceEqual(otherState._dec);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 31;
                hash = hash * 47 + _currentNumbers.GetHashCode();
                hash = hash * 19 + _goal.GetHashCode();
                hash = hash * 53 + _inc.GetHashCode();
                hash = hash * 97 + _dec.GetHashCode();
                return hash;
            }
        }
    }

    List<int> BFS()
    {
        int goalNumber = bomb.GetSerialNumberNumbers().First();
        State firstState = new State(keyVals, new[] { goalNumber, goalNumber, goalNumber, goalNumber }, keyInc, keyDec);
        Queue<State> queue = new Queue<State>();
        queue.Enqueue(firstState);
        List<State> visited = new List<State>();
        while (queue.Count != 0)
        {
            State currentState = queue.Dequeue();
            if (currentState.IsGoal())
            {
                List<int> indicesToPress = new List<int>();
                while (currentState.PreviousState != null)
                {
                    indicesToPress.Add(currentState.IndexToPress);
                    currentState = currentState.PreviousState;
                }
                indicesToPress.Reverse();
                return indicesToPress;
            }
            foreach (State newState in currentState.GetSuccessors())
            {
                if (!queue.Contains(newState) && !visited.Contains(newState))
                    queue.Enqueue(newState);
            }
        }
        throw new Exception("Error: Could not find a solution.");
    }
}