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
            pressed.AddInteractionPunch();
            audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, pressed.transform);
            keyVals[keyInc[Array.IndexOf(buttons, pressed)]] += 1;
            keyVals[keyDec[Array.IndexOf(buttons, pressed)]] -= 1;
            if (keyVals[keyInc[Array.IndexOf(buttons, pressed)]] > 9)
                keyVals[keyInc[Array.IndexOf(buttons, pressed)]] = 0;
            if (keyVals[keyDec[Array.IndexOf(buttons, pressed)]] < 0)
                keyVals[keyDec[Array.IndexOf(buttons, pressed)]] = 9;
            texts[keyInc[Array.IndexOf(buttons, pressed)]].text = keyVals[keyInc[Array.IndexOf(buttons, pressed)]].ToString();
            texts[keyDec[Array.IndexOf(buttons, pressed)]].text = keyVals[keyDec[Array.IndexOf(buttons, pressed)]].ToString();
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
        while (!moduleSolved)
        {
            buttons[UnityEngine.Random.Range(0, 4)].OnInteract();
            yield return new WaitForSeconds(0.02f);
        }
    }
}