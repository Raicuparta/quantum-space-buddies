﻿using QSB.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace QSB.ConversationSync
{
    public class ConversationManager : MonoBehaviour
    {
        public static ConversationManager Instance { get; private set; }

        void Start()
        {
            Instance = this;
        }

        public void SendPlayerOption(string text)
        {
            GlobalMessenger<int, string, ConversationType>.FireEvent(EventNames.QSBConversation, -1, text, ConversationType.PLAYER);
        }

        public void SendCharacterDialogue(int id, string text)
        {
            GlobalMessenger<int, string, ConversationType>.FireEvent(EventNames.QSBConversation, id, text, ConversationType.CHARACTER);
        }
    }
}
