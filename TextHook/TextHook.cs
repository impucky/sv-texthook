using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Quests;
using TextCopy;

namespace TextHook
{
    public class TextHook : Mod
    {
        public override void Entry(IModHelper helper)
        {
            //helper.Events.Input.ButtonPressed += this.OnButtonPressed;
            helper.Events.Display.MenuChanged += this.OnMenuChanged;
            helper.Events.Input.ButtonPressed += this.OnButtonPressed;
        }

        private DialogueBox? charDialogueBox;
        private QuestLog? activeQuestLog;
        private string lastViewedQuest = "";
        private void OnMenuChanged(object? sender, MenuChangedEventArgs e)
        {
            if (e.NewMenu is DialogueBox dialogueBox)
            {
                // Character dialogue
                if (dialogueBox.characterDialogue is { } characterDialogue)
                {
                    // Keep a reference to listen to clicks
                    charDialogueBox = dialogueBox;
                    string? currentDialogue = characterDialogue.getCurrentDialogue();
                    if (!string.IsNullOrEmpty(currentDialogue))
                    {
                        ClipboardService.SetText(currentDialogue);
                    }
                }
                // Non-character dialogue (TV, signs...)
                // Doesn't need a reference since it triggers OnMenuChanged for every line
                else if (dialogueBox.dialogues is List<string> dialogues && dialogues.Count > 0)
                {
                    string formattedDialogue = string.Join(" ", dialogues)
                                                     .Replace("^", "\n")
                                                     .Replace("\r\n", "");
                    // Append multiple-choice options
                    if (dialogueBox.responses is Response[] responses && responses.Length > 0)
                    {
                        foreach (var response in responses)
                        {
                            formattedDialogue += $"\n- {response.responseText}";
                        }
                    }
                    ClipboardService.SetText(formattedDialogue);
                }
            }
            // Pierre's 掲示板
            else if (e.NewMenu is Billboard billboard)
            {
                if (billboard.acceptQuestButton.visible is true)
                {
                    Quest? questOfTheDay = StardewValley.Utility.getQuestOfTheDay();

                    if (questOfTheDay != null)
                    {
                        ClipboardService.SetText(questOfTheDay.questDescription);
                    }
                }
            }
            // Mailbox
            else if (e.NewMenu is LetterViewerMenu letterViewerMenu)
            {
                if (letterViewerMenu.mailMessage is List<string> letter && letter.Count > 0)
                {
                    string formattedLetter = string.Join(" ", letter)
                                                     .Replace("^", "\n")
                                                     .Replace("\r\n", "");
                    ClipboardService.SetText(formattedLetter);
                }
            }

            else if (e.NewMenu is QuestLog questLog)
            {
                activeQuestLog = questLog;
            }
            else
            {
                activeQuestLog = null;
            }
        }
        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            // Clicking through dialogue
            if (charDialogueBox is not null && charDialogueBox.characterDialogue is { } characterDialogue)
            {
                string? currentDialogue = characterDialogue.getCurrentDialogue();
                if (!string.IsNullOrEmpty(currentDialogue))
                {
                    // Append multiple-choice options - Should work for events but untested
                    if (charDialogueBox.responses is Response[] responses && responses.Length > 0)
                    {
                        foreach (var response in responses)
                        {
                            currentDialogue += $"\n- {response.responseText}";
                        }
                    }
                    ClipboardService.SetText(currentDialogue);
                }
                // Stop on the final dialogue and unsubscribe
                if (characterDialogue.isOnFinalDialogue())
                {
                    charDialogueBox = null;

                }
                // In Quest log
                if (activeQuestLog is not null)
                {
                    var questField = typeof(QuestLog).GetField("_shownQuest", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (questField?.GetValue(activeQuestLog) is IQuest shownQuest)
                    {
                        string name = shownQuest.GetName();
                        // Avoid repeat logs
                        if (name != lastViewedQuest)
                        {
                            lastViewedQuest = name;
                            ClipboardService.SetText(name + "\n" + shownQuest.GetDescription());
                        }
                    }
                }
            }

        }
    }
}