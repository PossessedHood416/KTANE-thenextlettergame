using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;
using Math = ExMath;

public class NextLetterGame : MonoBehaviour {

   public KMBombInfo Bomb;
   public KMAudio Audio;
   public KMNeedyModule Needy;
   public KMSelectable NeedySelctable;

   static int ModuleIdCounter = 1;
   int ModuleId;
   private bool ModuleSolved;

   public KMSelectable SubButton;
   public TextMesh Display;
   public TextMesh SubText;

   private bool isFocused = false; //[!!!]
   private bool isActive = false;

   //rember all caps A0Z25
   public string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ", keyboardLayout = "QWERTYUIOPASDFGHJKLZXCVBNM";
   private char Call = 'A';
   private char Response = '?';
   private int CallIndex = 0;

   bool TwitchPlaysActive;

   void Awake () { //Avoid doing calculations in here regarding edgework. Just use this for setting up buttons for simplicity.
      ModuleId = ModuleIdCounter++;

      NeedySelctable.OnFocus += delegate { isFocused = true; };
      NeedySelctable.OnDefocus += delegate { isFocused = false; };

      SubButton.OnInteract += delegate () { ButtonPress(); return false; };

      Needy.OnNeedyActivation += OnNeedyActivation;
      Needy.OnNeedyDeactivation += OnNeedyDeactivation;
      Needy.OnTimerExpired += OnTimerExpired;
   }

   void ButtonPress (){
      SubButton.AddInteractionPunch();
      Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, SubButton.transform);
      if(!isFocused || !isActive) return;
      OnTimerExpired();
   }

   protected void OnNeedyActivation () { //Shit that happens when a needy turns on.
      //i love type coercion
      isActive = true;
      CallIndex = Rnd.Range(0,26);
      Call = Alphabet[CallIndex];
      Response = '?';
      SubText.text = "!";

      Display.text = Call + "" + Response;
      Debug.LogFormat("[Next Letter Game #{0}] What comes after {1}?", ModuleId, Call);

      if(TwitchPlaysActive){
         Needy.SetNeedyTimeRemaining(30f);   
      }
   }

   protected void OnNeedyDeactivation () { //blah blah blah
      Display.text = "--";
      SubText.text = "-";
      isActive = false;
   }

   protected void OnTimerExpired () { //Shit that happens when a needy turns off due to running out of time.
      if(!isActive) return;
      OnNeedyDeactivation();

      if(Alphabet[(CallIndex+1) % 26] != Response){
         Debug.LogFormat("[Next Letter Game #{0}] Answered {1}, Wrong! Answer should have been {2}.", ModuleId, Response, Alphabet[(CallIndex+1) % 26]);
         Needy.HandleStrike();
      } else {
         Debug.LogFormat("[Next Letter Game #{0}] Answered {1}, Right!", ModuleId, Response);
      }

      Needy.HandlePass();
   }

   void Start () { //Shit that you calculate, usually a majority if not all of the module
      Needy.SetResetDelayTime(50f, 70f);
   }

   void Update () { //Shit that happens at any point after initialization
      //congrats on solve
      if(Bomb.GetSolvableModuleNames().Count == Bomb.GetSolvedModuleNames().Count){
         Display.text = "GG";
         return;
      }

      if(!isFocused || !isActive){
         return;
      }

      //yoinked from ciphers
      for (int ltr = 0; ltr < 26; ltr++){
			if (Input.GetKeyDown(((char)('a' + ltr)).ToString())){
            Response = Alphabet[ltr];
            break;
         }
      }
      Display.text = Call + "" + Response;   
   }

#pragma warning disable 414
   private readonly string TwitchHelpMessage = @"Use !{0} A to input 'A'. Use !{0} * to submit. Use !{0} A* to combine the commands.";
#pragma warning restore 414

   IEnumerator ProcessTwitchCommand (string Command) {
      Command = Command.Trim().ToUpper();
      isFocused = true;
      yield return null;

      if(Command.Length > 2 || Command.Length < 1){
         yield return "sendtochaterror Invalid length, input 1-2 characters please.";
         yield break;
      }

      for(int i = 0; i < Command.Length; i++){
         if(Alphabet.IndexOf(Command[i]) == -1 && '*' != Command[i]){
            yield return "sendtochaterror Invalid input, use a letter or '*'.";
            yield break;
         }
      }

      for(int i = 0; i < Command.Length; i++){

         if(Command[i] == '*'){
            SubButton.OnInteract();
         } else {
            Response = Command[i];
         }
         yield return new WaitForSeconds(.2f);
      }
      yield break;
   }

   void TwitchHandleForcedSolve () { //Void so that autosolvers go to it first instead of potentially striking due to running out of time.
      StartCoroutine(HandleAutosolver());
   }

   IEnumerator HandleAutosolver () {
      isFocused = true;
      
      while(true){

         while(!isActive){
            yield return null;
         }

         yield return new WaitForSeconds(0.2f);
         Response = Alphabet[(CallIndex+1) % 26];
         yield return new WaitForSeconds(0.2f);
         SubButton.OnInteract();
      }
      yield return null;
   }
}
