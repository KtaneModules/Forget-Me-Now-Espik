using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;

public class ForgetMeNow : MonoBehaviour {
    public KMAudio Audio;
    public KMBombInfo Bomb;
    public KMBombModule Module;

    public KMSelectable[] Buttons;
    public Renderer[] Lights;
    public Material[] LightColors;

    public TextMesh DisplayScreen;
    public TextMesh InputScreen;
    public TextMesh StageScreen;

    // Logging info
    private static int moduleIdCounter = 1;
    private int moduleId;

    // Solving info
    private int lastDigit = 0; // Last digit of serial number
    private int firstDigit = 0; // First digit of serial number
    private int moduleCount = 0; // Module count

    private int firstButtonPressed = 0; // First button pressed on the module
    private int litButton = -1; // Lit light for each button

    private int[] displayDigits; // Displayed digits
    private int[] addedDigits; // Added digits
    private int[] solutionDigits; // Solution digits

    private int stage = 0; // Stage number

    private const int STAGES = 48;

    // "Here we go!" voice clip
    private bool hereWeGo = false;

    private int moduleStatus = 0;
    /* 0: Unactivated
     * 1: Activated
     * 2: Input mode
     * 3: Solved
     */

    // Equations
    private int[] fOfX = { 3, 4, 9, 6, 1, 9, 7, 4, 9, 1, 7, 9, 5, 6, 9, 8, 0, 9, 0, 0, 0};
    private int[] gOfX = { 2, 3, 4, 4, 4, 5, 5, 5, 6, 6, 6, 8, 8, 8, 10, 10, 10, 10, 12, 12, 12};
    private int[] hOfX = { 1, 2, 3, 3, 5, 5, 7, 7, 10, 10, 12, 12, 15, 15, 15, 15, 15, 15, 15, 15, 15};

    // Stage display delay
    private float[] delay = { 2.0f, 1.75f, 1.75f, 1.75f, 1.5f, 1.5f, 1.5f, 1.5f, 1.5f, 1.5f, 1.5f, 1.5f, 1.25f, 1.25f, 1.25f, 1.25f, 1.25f, 1.25f, 1.25f, 1.25f, 1.0f };

    // Ran as bomb loads
    private void Awake() {
        moduleId = moduleIdCounter++;

        // Delegation
        for (int i = 0; i < Buttons.Length; i++) {
            int j = i;
            Buttons[i].OnInteract += delegate () {
                ButtonPress(j);
                return false;
            };
        }
    }

    // Gets edgework and sets up calculations
    private void Start () {
        moduleCount = STAGES;
        moduleCount = Bomb.GetSolvableModuleNames().Count(); // Uncomment this to get the stages properly

        lastDigit = Bomb.GetSerialNumberNumbers().Last();
        firstDigit = Bomb.GetSerialNumberNumbers().First();

        // Prepares the strings of digits
        displayDigits = new int[moduleCount];
        addedDigits = new int[moduleCount];
        solutionDigits = new int[moduleCount];

        // Clears the screens
        DisplayScreen.text = "";
        InputScreen.text = "";
        StageScreen.text = "";
        
        Debug.LogFormat("[Forget Me Now #{0}] Press any button to activate the module.", moduleId);
    }


    // Creates the strings of digits
    private void CreateSequences() {
        // In case some weird error occurs
        if (moduleCount == 0) {
            GetComponent<KMBombModule>().HandlePass();
            Debug.LogFormat("[Forget Me Now #{0}] Some error occured where the module count is 0. Automatically solving.", moduleId);
        }

        for (int i = 0; i < moduleCount; i++) {
            displayDigits[i] = UnityEngine.Random.Range(0, 10);

            // Previous two digits
            int prevDigit1 = 0;
            int prevDigit2 = 0;

            if (i == 0) { // Stage 1
                prevDigit1 = firstButtonPressed;
                prevDigit2 = lastDigit;
            }

            else if (i == 1) { // Stage 2
                prevDigit1 = solutionDigits[i - 1];
                prevDigit2 = firstButtonPressed;
            }

            else { // Stage 3+
                prevDigit1 = solutionDigits[i - 2];
                prevDigit2 = solutionDigits[i - 1];
            }


            // Calculates the added digit
            if (prevDigit1 == 0 || prevDigit2 == 0) { // If either of the previous two digits are 0
                if (i > 20) {
                    addedDigits[i] = (int) Math.Ceiling((double) hOfX[20] * firstDigit / 5.0);
                }

                else {
                    addedDigits[i] = (int) Math.Ceiling((double) hOfX[i] * firstDigit / 5.0);
                }
            }

            else if (prevDigit1 % 2 == 0 && prevDigit2 % 2 == 0) { // If both the previous two digits are even
                if (i > 20)
                    addedDigits[i] = Math.Abs(gOfX[20] * 4 - moduleCount);

                else
                    addedDigits[i] = Math.Abs(gOfX[i] * 4 - moduleCount);
            }

            else { // Otherwise
                if (i > 20)
                    addedDigits[i] = prevDigit1 + prevDigit2 + fOfX[20];

                else
                    addedDigits[i] = prevDigit1 + prevDigit2 + fOfX[i];
            }


            // Adds the digits
            solutionDigits[i] = (displayDigits[i] + addedDigits[i]) % 10;
        }

        // Logging
        string displayLogger = "";
        string solutionLogger = "";

        for (int i = 0; i < moduleCount; i++) {
            if (i % 3 == 0 && i != 0) {
                displayLogger += " ";
                solutionLogger += " ";
            }

            displayLogger += displayDigits[i].ToString();
            solutionLogger += solutionDigits[i].ToString();
        }

        Debug.LogFormat("[Forget Me Now #{0}] Stage count: {1}.", moduleId, moduleCount);
        Debug.LogFormat("[Forget Me Now #{0}] Displayed digits: {1}", moduleId, displayLogger);
        Debug.LogFormat("[Forget Me Now #{0}] Solution digits: {1}", moduleId, solutionLogger);
    }


    // Button press
    private void ButtonPress(int i) {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, gameObject.transform);
        Buttons[i].AddInteractionPunch(0.2f);

        bool struck = false;

        switch (moduleStatus) {
            case 0: // Not activated
                firstButtonPressed = i;
                moduleStatus = 1;
                Debug.LogFormat("[Forget Me Now #{0}] The module activated by pressing button '{1}'. Let 'er rip!", moduleId, firstButtonPressed);
                CreateSequences();
                StartCoroutine(DisplayDigits());
                break;

            case 1: // Activated
                Debug.LogFormat("[Forget Me Now #{0}] The module struck because a button was pressed when digits were still being displayed!", moduleId);
                GetComponent<KMBombModule>().HandleStrike();
                break;

            case 2: // Input mode
                if (i == solutionDigits[stage]) {
                    stage++;
                    litButton = -1;
                    UpdateLED();
                    StageScreen.text = "--";
                    DisplayInputScreen();
                }

                else {
                    Debug.LogFormat("[Forget Me Now #{0}] The module struck from an incorrect digit entered at stage {1}. The displayed digit was a {2} for that stage.", moduleId, stage + 1, displayDigits[stage]);
                    Audio.PlaySoundAtTransform("FMNow_ThisIsntCorrect", transform);
                    GetComponent<KMBombModule>().HandleStrike();
                    litButton = displayDigits[stage];
                    UpdateLED();
                    DisplayStage(stage);
                    hereWeGo = false;
                    struck = true;
                }

                // Solving
                if (stage == moduleCount) {
                    Debug.LogFormat("[Forget Me Now #{0}] Module solved!", moduleId);
                    moduleStatus = 3;
                    //Audio.PlaySoundAtTransform("FMNow_WeDidItReddit", transform); // Unused until I find a sound effect that says "We did it, Reddit!"
                    Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, gameObject.transform);
                    GetComponent<KMBombModule>().HandlePass();
                    hereWeGo = false;
                }

                // Voice clip
                if (hereWeGo == true) {
                    Audio.PlaySoundAtTransform("FMNow_HereWeGo", transform);
                    hereWeGo = false;
                }

                if (struck == true)
                    hereWeGo = true;

                break;

            default: // Do nothing
                break;
        }
    }


    // Displays the LED by the button
    private void UpdateLED() {
        for (int i = 0; i < Lights.Length; i++) {
            if (litButton == i)
                Lights[i].material = LightColors[1];

            else
                Lights[i].material = LightColors[0];
        }
    }

    // Displays the digits
    private IEnumerator DisplayDigits() {
        for (int i = 0; i < moduleCount && moduleStatus == 1; i++) {
            // Displays stage number
            DisplayStage(i);

            // Displays number
            DisplayScreen.text = displayDigits[i].ToString();

            // Plays a sound for each stage
            if (i >= 24)
                Audio.PlaySoundAtTransform("FMNow_Groove", transform);

            else
                Audio.PlaySoundAtTransform("FMNow_Bass", transform);

            // Delay between the displayed digits
            if (i > 20)
                yield return new WaitForSeconds(delay[20]);

            else
                yield return new WaitForSeconds(delay[i]);
        }

        // Displaying is finished
        InitiateInputScreen();
        Debug.LogFormat("[Forget Me Now #{0}] The sequence of digits has finished displaying. Input mode is now active.", moduleId);
        // TP Force-solve command
        if (moduleStatus == 2)
            StartCoroutine(Solver());

        else
            moduleStatus = 2;
    }

    private void InitiateInputScreen() {
        StageScreen.text = "--";
        DisplayScreen.text = "";
        DisplayInputScreen();
        Audio.PlaySoundAtTransform("FMNow_Bass", transform);
        hereWeGo = true;
    }


    // Displays the input screen
    private void DisplayInputScreen() {
        string displayedText = "";

        // Displays in two groups of 12 stages.
        int currentStage = stage;
        int startingStage = 0;

        while (currentStage > 24) {
            currentStage -= 12;
            startingStage += 12;
        }


        // Displays blank spaces and entered digits
        for (int i = startingStage; i < Math.Min(startingStage + 24, moduleCount); i++) {
            string digit = "-";

            // Correct digits entered
            if (i < stage)
                digit = solutionDigits[i].ToString();

            // Spaces
            if (i > startingStage) {
                if (i % 3 == 0) {
                    if (i % 12 == 0)
                        displayedText += "\n";

                    else
                        displayedText += " ";
                }
            }

            displayedText += digit;
        }

        InputScreen.text = displayedText;
    }

    // Displays stage number
    private void DisplayStage(int i) {
        int stageNo = (i + 1) % 100;

        if (moduleCount < 10 || stageNo >= 10)
            StageScreen.text = stageNo.ToString();

        else
            StageScreen.text = 0 + stageNo.ToString();
    }

    
    // Twitch Plays - borrowed code from Forget Me Not with some edits
#pragma warning disable 414
    private string TwitchHelpMessage = "Press one button with \"!{0} press 5\", or enter the sequence with \"!{0} press 531820...\". You may use spaces and commas.";
#pragma warning restore 414

    public void TwitchHandleForcedSolve() {
        Debug.LogFormat("[Forget Me Now #{0}] Module forcibly solved.", moduleId);

        if (moduleStatus == 2)
            StartCoroutine(Solver());

        else if (moduleStatus == 0) {
            InitiateInputScreen();
            moduleStatus = 2;
            StartCoroutine(Solver());
        }

        else if (moduleStatus == 1)
            moduleStatus = 2;
    }

    private IEnumerator Solver() {
        while (stage < solutionDigits.Length) {
            yield return new WaitForSeconds(0.05f);
            Buttons[solutionDigits[stage]].OnInteract();
        }
    }

    private int GetDigit(char c) {
        switch (c) {
            case '0': return 0;
            case '1': return 1;
            case '2': return 2;
            case '3': return 3;
            case '4': return 4;
            case '5': return 5;
            case '6': return 6;
            case '7': return 7;
            case '8': return 8;
            case '9': return 9;
            default: return -1;
        }
    }

    public IEnumerator ProcessTwitchCommand(string cmd) {
        if (stage >= solutionDigits.Length) yield break;
        cmd = cmd.ToLowerInvariant();

        int cut;
        if (cmd.StartsWith("submit ")) cut = 7;
        else if (cmd.StartsWith("press ")) cut = 6;
        else {
            yield return "sendtochaterror Use either 'submit' or 'press' followed by a number sequence.";
            yield break;
        }

        List<int> digits = new List<int>();
        char[] strSplit = cmd.Substring(cut).ToCharArray();
        foreach (char c in strSplit) {
            if (!"0123456789 ,".Contains(c)) {
                yield return "sendtochaterror Invalid character in number sequence: '" + c + "'.\nValid characters are 0-9, space, and comma.";
                yield break;
            }

            int d = GetDigit(c);
            if (d != -1) digits.Add(d);
        }
        if (digits.Count == 0) yield break;
        if (digits.Count > (solutionDigits.Length - stage)) {
            yield return "sendtochaterror Too many digits submitted.";
            yield break;
        }

        int progress = moduleCount;
        if (progress < solutionDigits.Length) {
            yield return "Forget Me Now";
            Buttons[digits[0]].OnInteract();
            yield break;
        }
        yield return "Forget Me Now";

        foreach (int d in digits) {
            Buttons[d].OnInteract();
            yield return new WaitForSeconds(0.05f);
        }
        yield break;
    }
}