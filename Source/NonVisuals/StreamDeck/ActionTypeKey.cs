﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using ClassLibraryCommon;
using Newtonsoft.Json;
using NonVisuals.Interfaces;
using NonVisuals.Saitek;

namespace NonVisuals.StreamDeck
{
    [Serializable]
    public class ActionTypeKey : KeyBinding, IStreamDeckButtonTypeBase, IStreamDeckButtonAction
    {
        public EnumStreamDeckActionType ActionType => EnumStreamDeckActionType.KeyPress;
        public bool IsRepeatable() => true;
        private EnumStreamDeckButtonNames _streamDeckButtonName;
        [NonSerialized]
        private StreamDeckPanel _streamDeckPanel;


        public ActionTypeKey(StreamDeckPanel streamDeckPanel)
        {
            _streamDeckPanel = streamDeckPanel;
        }

        
        public new int GetHash()
        {
            unchecked
            {
                var result = _streamDeckButtonName.GetHashCode();
                result = (result * 397) ^ base.GetHash();
                return result;
            }
        }

        [JsonIgnore]
        public string ActionDescription
        {
            get
            {
                var stringBuilder = new StringBuilder(100);
                stringBuilder.Append("Key press");
                if (OSKeyPress != null)
                {
                    stringBuilder.Append(" ").Append(OSKeyPress.GetKeyPressInformation());
                }

                return stringBuilder.ToString();
            }
        }


        public bool IsRunning()
        {
            return OSKeyPress.IsRunning();
        }

        public void Execute(CancellationToken threadCancellationToken)
        {
            Common.PlaySoundFile(false, SoundFile, Volume);
            OSKeyPress.Execute(threadCancellationToken);
        }


        public EnumStreamDeckButtonNames StreamDeckButtonName
        {
            get => _streamDeckButtonName;
            set => _streamDeckButtonName = value;
        }

        public static HashSet<ActionTypeKey> SetNegators(HashSet<ActionTypeKey> keyBindings)
        {
            /*if (keyBindings == null)
            {
                return null;
            }
            foreach (var keyBindingStreamDeck in keyBindings)
            {
                //Clear all negators
                keyBindingStreamDeck.OSKeyPress.NegatorOSKeyPresses.Clear();

                foreach (var keyBinding in keyBindings)
                {
                    if (keyBinding != keyBindingStreamDeck && keyBinding.EnumStreamDeckButtonName == keyBindingStreamDeck.EnumStreamDeckButtonName && keyBinding.WhenTurnedOn != keyBindingStreamDeck.WhenTurnedOn)
                    {
                        keyBindingStreamDeck.OSKeyPress.NegatorOSKeyPresses.Add(keyBinding.OSKeyPress);
                    }
                }
            }*/
            return keyBindings;
        }

        internal override void ImportSettings(string settings) { }

        public override string ExportSettings()
        {
            return null;
        }

        [JsonIgnore]
        public StreamDeckPanel StreamDeckPanelInstance
        {
            get => _streamDeckPanel;
            set => _streamDeckPanel = value;
        }

        public string SoundFile { get; set; }
        public double Volume { get; set; }
        public int Delay { get; set; }
        public bool HasSound => !string.IsNullOrEmpty(SoundFile) && File.Exists(SoundFile);
        public void PlaySound()
        {
            Common.PlaySoundFile(false, SoundFile, Volume);
        }
    }
}
