﻿using QSB.WorldSync;
using System.Collections.Generic;
using UnityEngine;

namespace QSB.ConversationSync
{
    public static class ConversationPatches
    {
        public static void StartConversation(CharacterDialogueTree __instance)
        {
            var index = WorldRegistry.OldDialogueTrees.FindIndex(x => x == __instance);
            PlayerRegistry.LocalPlayer.CurrentDialogueID = index;
            ConversationManager.Instance.SendStart(index);
        }

        public static void EndConversation()
        {
            ConversationManager.Instance.SendEnd(PlayerRegistry.LocalPlayer.CurrentDialogueID);
            ConversationManager.Instance.CloseBoxCharacter(PlayerRegistry.LocalPlayer.CurrentDialogueID);
            PlayerRegistry.LocalPlayer.CurrentDialogueID = -1;
            ConversationManager.Instance.CloseBoxPlayer();
        }

        public static bool InputDialogueOption(int optionIndex, DialogueBoxVer2 ____currentDialogueBox)
        {
            if (optionIndex < 0)
            {
                // in a page where there is no selectable options
                ConversationManager.Instance.CloseBoxPlayer();
                return true;
            }

            var selectedOption = ____currentDialogueBox.OptionFromUIIndex(optionIndex);
            ConversationManager.Instance.SendPlayerOption(selectedOption.Text);
            return true;
        }

        public static void GetNextPage(string ____name, List<string> ____listPagesToDisplay, int ____currentPage)
        {
            var key = ____name + ____listPagesToDisplay[____currentPage];
            // Sending key so translation can be done on client side - should make different language-d clients compatible
            QSB.Helper.Events.Unity.RunWhen(() => PlayerRegistry.LocalPlayer.CurrentDialogueID != -1,
                () => ConversationManager.Instance.SendCharacterDialogue(PlayerRegistry.LocalPlayer.CurrentDialogueID, key));
        }

        public static bool OnAnimatorIK(float ___headTrackingWeight,
            bool ___lookOnlyWhenTalking,
            bool ____playerInHeadZone,
            bool ____inConversation,
            ref float ____currentLookWeight,
            ref Vector3 ____currentLookTarget,
            DampedSpring3D ___lookSpring,
            Animator ____animator,
            CharacterDialogueTree ____dialogueTree)
        {
            var playerId = ConversationManager.Instance.GetPlayerTalkingToTree(____dialogueTree);
            Vector3 position;
            if (playerId == uint.MaxValue)
            {
                position = Locator.GetActiveCamera().transform.position;
            }
            else
            {
                position = PlayerRegistry.GetPlayer(playerId).Camera.transform.position;
            }
            float b = ___headTrackingWeight * (float)Mathf.Min(1, (!___lookOnlyWhenTalking) ? ((!____playerInHeadZone) ? 0 : 1) : ((!____inConversation || !____playerInHeadZone) ? 0 : 1));
            ____currentLookWeight = Mathf.Lerp(____currentLookWeight, b, Time.deltaTime * 2f);
            ____currentLookTarget = ___lookSpring.Update(____currentLookTarget, position, Time.deltaTime);
            ____animator.SetLookAtPosition(____currentLookTarget);
            ____animator.SetLookAtWeight(____currentLookWeight);
            return false;

        }

        public static void AddPatches()
        {
            QSB.Helper.HarmonyHelper.AddPostfix<DialogueNode>("GetNextPage", typeof(ConversationPatches), nameof(GetNextPage));
            QSB.Helper.HarmonyHelper.AddPrefix<CharacterDialogueTree>("InputDialogueOption", typeof(ConversationPatches), nameof(InputDialogueOption));
            QSB.Helper.HarmonyHelper.AddPostfix<CharacterDialogueTree>("StartConversation", typeof(ConversationPatches), nameof(StartConversation));
            QSB.Helper.HarmonyHelper.AddPostfix<CharacterDialogueTree>("EndConversation", typeof(ConversationPatches), nameof(EndConversation));
            QSB.Helper.HarmonyHelper.AddPrefix<CharacterAnimController>("OnAnimatorIK", typeof(ConversationPatches), nameof(OnAnimatorIK));
        }
    }
}
