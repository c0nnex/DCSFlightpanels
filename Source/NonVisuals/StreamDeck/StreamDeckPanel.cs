﻿using System;
using System.Collections.Generic;
using System.Threading;
using ClassLibraryCommon;
using DCS_BIOS;
using NonVisuals.StreamDeck;
using OpenMacroBoard.SDK;
using StreamDeckSharp;

namespace NonVisuals
{
    public class StreamDeckPanel : GamingPanel
    {
        private IStreamDeckBoard _streamDeckBoard;
        private int _lcdKnobSensitivity;
        private volatile byte _knobSensitivitySkipper;
        private HashSet<DCSBIOSBindingStreamDeck> _dcsBiosBindings = new HashSet<DCSBIOSBindingStreamDeck>();
        private HashSet<DCSBIOSBindingLCDStreamDeck> _dcsBiosLcdBindings = new HashSet<DCSBIOSBindingLCDStreamDeck>();
        private HashSet<KeyBindingStreamDeck> _keyBindings = new HashSet<KeyBindingStreamDeck>();
        private HashSet<OSCommandBindingStreamDeck> _osCommandBindings = new HashSet<OSCommandBindingStreamDeck>();
        private HashSet<BIPLinkStreamDeck> _bipLinks = new HashSet<BIPLinkStreamDeck>();
        private StreamDeckLayerHandler _streamDeckLayerHandler = new StreamDeckLayerHandler();
        private readonly object _lcdLockObject = new object();
        private readonly object _lcdDataVariablesLockObject = new object();
        
        private long _doUpdatePanelLCD;

        public StreamDeckPanel(HIDSkeleton hidSkeleton):base(GamingPanelEnum.StreamDeck, hidSkeleton)
        {
            Startup();
            _streamDeckBoard = StreamDeckSharp.StreamDeck.OpenDevice(hidSkeleton.InstanceId, false);
        }

        public sealed override void Startup()
        {
            try
            {
                StartListeningForPanelChanges();
            }
            catch (Exception ex)
            {
                Common.DebugP("StreamDeck.StartUp() : " + ex.Message);
                Common.LogError(321654, ex);
            }
        }

        public override void Shutdown()
        {
            try
            {
                Closed = true;
            }
            catch (Exception e)
            {
                SetLastException(e);
            }
        }

        private void StreamDeckKeyHandler(object sender, KeyEventArgs e)
        {
            if (!(sender is IMacroBoard))
            {
                return;
            }

            var keyId = e.Key;
            if (e.IsDown)
            {
                switch (keyId)
                {
                    /*case 0:
                        //tens up
                        SetFreqArray(Eagle.CurrentFreq + 100, Eagle);
                        SendSrsInput(Xbox360Buttons.A);
                        break;
                    case 1:
                        SetFreqArray(Eagle.CurrentFreq + 10, Eagle);
                        SendSrsInput(Xbox360Buttons.X);
                        break;
                    case 2:
                        //first decimal up
                        SetFreqArray(Eagle.CurrentFreq + 1, Eagle);
                        SendSrsInput(Xbox360Buttons.Up);
                        break;
                    case 10:
                        //tens down
                        SetFreqArray(Eagle.CurrentFreq - 100, Eagle);
                        SendSrsInput(Xbox360Buttons.B);
                        break;
                    case 11:
                        //ones down
                        SetFreqArray(Eagle.CurrentFreq - 10, Eagle);
                        SendSrsInput(Xbox360Buttons.Y);
                        break;
                    case 12:
                        //first decimal down
                        SetFreqArray(Eagle.CurrentFreq - 1, Eagle);
                        SendSrsInput(Xbox360Buttons.Down);
                        break;*/
                }
            }
            //SetKeys(Eagle.FreqArray);
        }

        protected override void StartListeningForPanelChanges()
        {
            try
            {
                
            }
            catch (Exception ex)
            {
                Common.DebugP(ex.Message + "\n" + ex.StackTrace);
            }
        }

        public override void DcsBiosDataReceived(object sender, DCSBIOSDataEventArgs e)
        {
            if (SettingsLoading)
            {
                return;
            }
            UpdateCounter(e.Address, e.Data);
            foreach (var dcsbiosBindingLCD in _dcsBiosLcdBindings)
            {
                if (!dcsbiosBindingLCD.UseFormula && e.Address == dcsbiosBindingLCD.DCSBIOSOutputObject.Address)
                {
                    lock (_lcdDataVariablesLockObject)
                    {
                        var tmp = dcsbiosBindingLCD.CurrentValue;
                        dcsbiosBindingLCD.CurrentValue = (int)dcsbiosBindingLCD.DCSBIOSOutputObject.GetUIntValue(e.Data);
                        if (tmp != dcsbiosBindingLCD.CurrentValue)
                        {
                            Interlocked.Add(ref _doUpdatePanelLCD, 1);
                        }
                    }
                }
                else if (dcsbiosBindingLCD.UseFormula)
                {
                    if (dcsbiosBindingLCD.DCSBIOSOutputFormulaObject.CheckForMatch(e.Address, e.Data))
                    {
                        lock (_lcdDataVariablesLockObject)
                        {
                            var tmp = dcsbiosBindingLCD.CurrentValue;
                            dcsbiosBindingLCD.CurrentValue = dcsbiosBindingLCD.DCSBIOSOutputFormulaObject.Evaluate();
                            if (tmp != dcsbiosBindingLCD.CurrentValue)
                            {
                                Interlocked.Add(ref _doUpdatePanelLCD, 1);
                            }
                        }
                    }
                }
            }
        }

        public override void ImportSettings(List<string> settings)
        {
            SettingsLoading = true;
            //Clear current bindings
            ClearSettings();
            if (settings == null || settings.Count == 0)
            {
                return;
            }

            foreach (var setting in settings)
            {
                if (!setting.StartsWith("#") && setting.Length > 2 && setting.Contains(InstanceId) && setting.Contains(SettingsVersion()))
                {
                    if (setting.StartsWith("StreamDeckButton{"))
                    {
                        var keyBinding = new KeyBindingStreamDeck();
                        keyBinding.ImportSettings(setting);
                        _keyBindings.Add(keyBinding);
                    }
                    else if (setting.StartsWith("StreamDeckOS"))
                    {
                        var osCommand = new OSCommandBindingStreamDeck();
                        osCommand.ImportSettings(setting);
                        _osCommandBindings.Add(osCommand);
                    }
                    else if (setting.StartsWith("StreamDeckDCSBIOSControl{"))
                    {
                        var dcsBIOSBindingStreamDeck = new DCSBIOSBindingStreamDeck();
                        dcsBIOSBindingStreamDeck.ImportSettings(setting);
                        _dcsBiosBindings.Add(dcsBIOSBindingStreamDeck);
                    }
                    else if (setting.StartsWith("StreamDeckBIPLink{"))
                    {
                        var bipLinkStreamDeck = new BIPLinkStreamDeck();
                        bipLinkStreamDeck.ImportSettings(setting);
                        _bipLinks.Add(bipLinkStreamDeck);
                    }
                    else if (setting.StartsWith("StreamDeckDCSBIOSControlLCD{"))
                    {
                        var dcsBIOSBindingLCDStreamDeck = new DCSBIOSBindingLCDStreamDeck();
                        dcsBIOSBindingLCDStreamDeck.ImportSettings(setting);
                        _dcsBiosLcdBindings.Add(dcsBIOSBindingLCDStreamDeck);
                    }
                    else if (setting.StartsWith("Layers{"))
                    {
                        _streamDeckLayerHandler.AddLayer(setting);
                    }
                }
            }
            SettingsLoading = false;
            _keyBindings = KeyBindingStreamDeck.SetNegators(_keyBindings);
            OnSettingsApplied();
        }

        public override List<string> ExportSettings()
        {
            if (Closed)
            {
                return null;
            }
            var result = new List<string>();

            foreach (var keyBinding in _keyBindings)
            {
                if (keyBinding.OSKeyPress != null)
                {
                    result.Add(keyBinding.ExportSettings());
                }
            }
            foreach (var osCommand in _osCommandBindings)
            {
                if (!osCommand.OSCommandObject.IsEmpty)
                {
                    result.Add(osCommand.ExportSettings());
                }
            }
            foreach (var dcsBiosBinding in _dcsBiosBindings)
            {
                if (dcsBiosBinding.DCSBIOSInputs.Count > 0)
                {
                    result.Add(dcsBiosBinding.ExportSettings());
                }
            }
            foreach (var dcsBiosBindingLCD in _dcsBiosLcdBindings)
            {
                if (dcsBiosBindingLCD.HasBinding)
                {
                    result.Add(dcsBiosBindingLCD.ExportSettings());
                }
            }
            foreach (var bipLink in _bipLinks)
            {
                if (bipLink.BIPLights.Count > 0)
                {
                    result.Add(bipLink.ExportSettings());
                }
            }
            
            result.Add(_streamDeckLayerHandler.ExportLayers());

            return result;
        }

        public string GetKeyPressForLoggingPurposes(StreamDeckButton streamDeckButton)
        {
            var result = "";
            foreach (var keyBinding in _keyBindings)
            {
                if (keyBinding.OSKeyPress != null && keyBinding.StreamDeckButton == streamDeckButton.Button && keyBinding.WhenTurnedOn == streamDeckButton.IsPressed)
                {
                    result = keyBinding.OSKeyPress.GetNonFunctioningVirtualKeyCodesAsString();
                }
            }
            return result;
        }
        
        public override void SavePanelSettings(object sender, ProfileHandlerEventArgs e)
        {
            e.ProfileHandlerEA.RegisterProfileData(this, ExportSettings());
        }

        public override void ClearSettings()
        {
            _keyBindings.Clear();
            _osCommandBindings.Clear();
            _dcsBiosBindings.Clear();
            _dcsBiosLcdBindings.Clear();
            _bipLinks.Clear();
            _streamDeckLayerHandler.ClearSettings();
        }

        protected override void GamingPanelKnobChanged(IEnumerable<object> hashSet)
        {
            //Set _selectedMode and LCD button statuses
            //and performs the actual actions for key presses
            // ADD METHOD ?
        }

        public void AddOrUpdateSingleKeyBinding(string layer, StreamDeckButtons streamDeckButton, string keys, KeyPressLength keyPressLength, bool whenTurnedOn)
        {
            if (string.IsNullOrEmpty(keys))
            {
                RemoveButtonFromList(layer, ControlListStreamDeck.KEYS, streamDeckButton, whenTurnedOn);
                SetIsDirty();
                return;
            }
            //This must accept lists
            var found = false;
            foreach (var keyBinding in _keyBindings)
            {
                if (layer == keyBinding.Layer && keyBinding.StreamDeckButton == streamDeckButton && keyBinding.WhenTurnedOn == whenTurnedOn)
                {
                    if (string.IsNullOrEmpty(keys))
                    {
                        keyBinding.OSKeyPress = null;
                    }
                    else
                    {
                        keyBinding.OSKeyPress = new KeyPress(keys, keyPressLength);
                    }
                    found = true;
                }
            }
            if (!found && !string.IsNullOrEmpty(keys))
            {
                var keyBinding = new KeyBindingStreamDeck();
                keyBinding.Layer = layer;
                keyBinding.StreamDeckButton = streamDeckButton;
                keyBinding.OSKeyPress = new KeyPress(keys, keyPressLength);
                keyBinding.WhenTurnedOn = whenTurnedOn;
                _keyBindings.Add(keyBinding);
            }
            Common.DebugP("Stream Deck _keybindings : " + _keyBindings.Count);
            _keyBindings = KeyBindingStreamDeck.SetNegators(_keyBindings);
            SetIsDirty();
        }

        public void AddOrUpdateOSCommandBinding(string layer, StreamDeckButtons streamDeckButton, OSCommand osCommand, bool whenTurnedOn)
        {
            //This must accept lists
            /*var found = false;

            foreach (var osCommandBinding in _osCommandBindings)
            {
                if (layer == osCommandBinding.Layer && osCommandBinding.StreamDeckButton == streamDeckButton && osCommandBinding.WhenTurnedOn == whenTurnedOn)
                {
                    osCommandBinding.OSCommandObject = osCommand;
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                var osCommandBindingPZ70 = new OSCommandBindingStreamDeck();
                osCommandBindingPZ70.StreamDeckButton = streamDeckButton;
                osCommandBindingPZ70.OSCommandObject = osCommand;
                osCommandBindingPZ70.WhenTurnedOn = whenTurnedOn;
                _osCommandBindings.Add(osCommandBindingPZ70);
            }
            SetIsDirty();*/
        }

        public void AddOrUpdateSequencedKeyBinding(string layer, string information, StreamDeckButtons streamDeckButton, SortedList<int, KeyPressInfo> sortedList, bool whenTurnedOn)
        {
            if (sortedList.Count == 0)
            {
                RemoveButtonFromList(layer, ControlListStreamDeck.KEYS, streamDeckButton, whenTurnedOn);
                SetIsDirty();
                return;
            }
            //This must accept lists
            var found = false;
            foreach (var keyBinding in _keyBindings)
            {
                if (layer == keyBinding.Layer && keyBinding.StreamDeckButton == streamDeckButton && keyBinding.WhenTurnedOn == whenTurnedOn)
                {
                    if (sortedList.Count == 0)
                    {
                        keyBinding.OSKeyPress = null;
                    }
                    else
                    {
                        keyBinding.OSKeyPress = new KeyPress(information, sortedList);
                    }
                    found = true;
                    break;
                }
            }
            if (!found && sortedList.Count > 0)
            {
                var keyBinding = new KeyBindingStreamDeck();
                keyBinding.Layer = layer;
                keyBinding.StreamDeckButton = streamDeckButton;
                keyBinding.OSKeyPress = new KeyPress(information, sortedList);
                keyBinding.WhenTurnedOn = whenTurnedOn;
                _keyBindings.Add(keyBinding);
            }
            _keyBindings = KeyBindingStreamDeck.SetNegators(_keyBindings);
            SetIsDirty();
        }

        public void AddOrUpdateDCSBIOSBinding(string layer, StreamDeckButtons streamDeckButton, List<DCSBIOSInput> dcsbiosInputs, string description, bool whenTurnedOn)
        {
            if (dcsbiosInputs.Count == 0)
            {
                RemoveButtonFromList(layer, ControlListStreamDeck.DCSBIOS, streamDeckButton, whenTurnedOn);
                SetIsDirty();
                return;
            }
            //This must accept lists
            var found = false;
            foreach (var dcsBiosBinding in _dcsBiosBindings)
            {
                if (layer == dcsBiosBinding.Layer && dcsBiosBinding.StreamDeckButton == streamDeckButton && dcsBiosBinding.WhenTurnedOn == whenTurnedOn)
                {
                    dcsBiosBinding.DCSBIOSInputs = dcsbiosInputs;
                    dcsBiosBinding.Description = description;
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                var dcsBiosBinding = new DCSBIOSBindingStreamDeck();
                dcsBiosBinding.Layer = layer;
                dcsBiosBinding.StreamDeckButton = streamDeckButton;
                dcsBiosBinding.DCSBIOSInputs = dcsbiosInputs;
                dcsBiosBinding.WhenTurnedOn = whenTurnedOn;
                dcsBiosBinding.Description = description;
                _dcsBiosBindings.Add(dcsBiosBinding);
            }
            SetIsDirty();
        }

        public void AddOrUpdateLCDBinding(string layer, DCSBIOSOutput dcsbiosOutput, StreamDeckButtons streamDeckButton)
        {
            var found = false;
            foreach (var dcsBiosBindingLCD in _dcsBiosLcdBindings)
            {
                if (layer == dcsBiosBindingLCD.Layer && dcsBiosBindingLCD.StreamDeckButton == streamDeckButton)
                {
                    dcsBiosBindingLCD.DCSBIOSOutputObject = dcsbiosOutput;
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                var dcsBiosBindingLCD = new DCSBIOSBindingLCDStreamDeck();
                dcsBiosBindingLCD.Layer = layer;
                dcsBiosBindingLCD.DCSBIOSOutputObject = dcsbiosOutput;
                _dcsBiosLcdBindings.Add(dcsBiosBindingLCD);
            }
            SetIsDirty();
        }

        public void AddOrUpdateLCDBinding(string layer, DCSBIOSOutputFormula dcsbiosOutputFormula, StreamDeckButtons streamDeckButton)
        {
            var found = false;
            foreach (var dcsBiosBindingLCD in _dcsBiosLcdBindings)
            {
                if (layer == dcsBiosBindingLCD.Layer && dcsBiosBindingLCD.StreamDeckButton == streamDeckButton)
                {
                    dcsBiosBindingLCD.DCSBIOSOutputFormulaObject = dcsbiosOutputFormula;
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                var dcsBiosBindingLCD = new DCSBIOSBindingLCDStreamDeck();
                dcsBiosBindingLCD.DCSBIOSOutputFormulaObject = dcsbiosOutputFormula;
                dcsBiosBindingLCD.StreamDeckButton = streamDeckButton;
                dcsBiosBindingLCD.Layer = layer;
                _dcsBiosLcdBindings.Add(dcsBiosBindingLCD);
            }
            SetIsDirty();
        }

        public void AddOrUpdateDCSBIOSLcdBinding(string layer, StreamDeckButtons streamDeckButton)
        {
            //Removes config
            foreach (var dcsBiosBindingLCD in _dcsBiosLcdBindings)
            {
                if (layer == dcsBiosBindingLCD.Layer && dcsBiosBindingLCD.StreamDeckButton == streamDeckButton)
                {
                    dcsBiosBindingLCD.DCSBIOSOutputObject = null;
                    break;
                }
            }
            SetIsDirty();
        }

        public void AddOrUpdateBIPLinkKeyBinding(string layer, StreamDeckButtons streamDeckButton, BIPLinkStreamDeck bipLinkStreamDeck, bool whenTurnedOn)
        {
            if (bipLinkStreamDeck.BIPLights.Count == 0)
            {
                RemoveButtonFromList(layer, ControlListStreamDeck.BIPS, streamDeckButton, whenTurnedOn);
                SetIsDirty();
                return;
            }
            //This must accept lists
            var found = false;

            foreach (var bipLink in _bipLinks)
            {
                if (layer == bipLink.Layer && bipLink.StreamDeckButton == streamDeckButton && bipLink.WhenTurnedOn == whenTurnedOn)
                {
                    bipLink.BIPLights = bipLinkStreamDeck.BIPLights;
                    bipLink.Description = bipLinkStreamDeck.Description;
                    bipLink.StreamDeckButton = streamDeckButton;
                    found = true;
                    break;
                }
            }
            if (!found && bipLinkStreamDeck.BIPLights.Count > 0)
            {
                bipLinkStreamDeck.StreamDeckButton = streamDeckButton;
                bipLinkStreamDeck.WhenTurnedOn = whenTurnedOn;
                bipLinkStreamDeck.Layer = layer;
                _bipLinks.Add(bipLinkStreamDeck);
            }
            SetIsDirty();
        }

        public void RemoveButtonFromList(string layer, ControlListStreamDeck controlListStreamDeck, StreamDeckButtons streamDeckButton, bool whenTurnedOn)
        {
            var found = false;
            if (controlListStreamDeck == ControlListStreamDeck.ALL || controlListStreamDeck == ControlListStreamDeck.KEYS)
            {
                foreach (var buttonBinding in _keyBindings)
                {
                    if (layer == buttonBinding.Layer && buttonBinding.StreamDeckButton == streamDeckButton && buttonBinding.WhenTurnedOn == whenTurnedOn)
                    {
                        buttonBinding.OSKeyPress = null;
                        found = true;
                    }
                }
            }
            if (controlListStreamDeck == ControlListStreamDeck.ALL || controlListStreamDeck == ControlListStreamDeck.DCSBIOS)
            {
                foreach (var dcsBiosBinding in _dcsBiosBindings)
                {
                    if (layer == dcsBiosBinding.Layer && dcsBiosBinding.StreamDeckButton == streamDeckButton && dcsBiosBinding.WhenTurnedOn == whenTurnedOn)
                    {
                        dcsBiosBinding.DCSBIOSInputs.Clear();
                        found = true;
                    }
                }
            }
            if (controlListStreamDeck == ControlListStreamDeck.ALL || controlListStreamDeck == ControlListStreamDeck.BIPS)
            {
                foreach (var bipLink in _bipLinks)
                {
                    if (layer == bipLink.Layer && bipLink.StreamDeckButton == streamDeckButton && bipLink.WhenTurnedOn == whenTurnedOn)
                    {
                        bipLink.BIPLights.Clear();
                        found = true;
                    }
                }
            }

            if (found)
            {
                SetIsDirty();
            }
        }

        private void StreamDeckButtonChanged(IEnumerable<object> hashSet)
        {
            if (!ForwardPanelEvent)
            {
                return;
            }

            foreach (var o in hashSet)
            {
                var streamDeckButton = (StreamDeckButton)o;

                var found = false;
                foreach (var keyBinding in _keyBindings)
                {
                    if (keyBinding.OSKeyPress != null && keyBinding.StreamDeckButton == streamDeckButton.Button && keyBinding.WhenTurnedOn == streamDeckButton.IsPressed)
                    {
                        keyBinding.OSKeyPress.Execute();
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    foreach (var dcsBiosBinding in _dcsBiosBindings)
                    {
                        if (dcsBiosBinding.DCSBIOSInputs.Count > 0 && dcsBiosBinding.StreamDeckButton == streamDeckButton.Button && dcsBiosBinding.WhenTurnedOn == streamDeckButton.IsPressed)
                        {
                            dcsBiosBinding.SendDCSBIOSCommands();
                            break;
                        }
                    }
                }
                foreach (var osCommand in _osCommandBindings)
                {
                    if (osCommand.OSCommandObject != null && osCommand.StreamDeckButton == streamDeckButton.Button && osCommand.WhenTurnedOn == streamDeckButton.IsPressed)
                    {
                        osCommand.OSCommandObject.Execute();
                        found = true;
                        break;
                    }
                }
                foreach (var bipLinkStreamDeck in _bipLinks)
                {
                    if (bipLinkStreamDeck.BIPLights.Count > 0 && bipLinkStreamDeck.StreamDeckButton == streamDeckButton.Button && bipLinkStreamDeck.WhenTurnedOn == streamDeckButton.IsPressed)
                    {
                        bipLinkStreamDeck.Execute();
                        break;
                    }
                }
            }
        }
        
        private void DeviceAttachedHandler()
        {
            Startup();
            //IsAttached = true;
        }

        private void DeviceRemovedHandler()
        {
            Shutdown();
            //IsAttached = false;
        }

        public DcsOutputAndColorBinding CreateDcsOutputAndColorBinding(SaitekPanelLEDPosition saitekPanelLEDPosition, PanelLEDColor panelLEDColor, DCSBIOSOutput dcsBiosOutput)
        {
            return null;
        }

        public HashSet<DCSBIOSBindingStreamDeck> DCSBiosBindings
        {
            get => _dcsBiosBindings;
            set => _dcsBiosBindings = value;
        }

        public HashSet<KeyBindingStreamDeck> KeyBindings
        {
            get => _keyBindings;
            set => _keyBindings = value;
        }

        public HashSet<BIPLinkStreamDeck> BIPLinkHashSet
        {
            get => _bipLinks;
            set => _bipLinks = value;
        }

        public HashSet<KeyBindingStreamDeck> KeyBindingsHashSet
        {
            get => _keyBindings;
            set => _keyBindings = value;
        }

        public HashSet<OSCommandBindingStreamDeck> OSCommandHashSet
        {
            get => _osCommandBindings;
            set => _osCommandBindings = value;
        }

        public HashSet<DCSBIOSBindingLCDStreamDeck> LCDBindings
        {
            get => _dcsBiosLcdBindings;
            set => _dcsBiosLcdBindings = value;
        }

        public int LCDKnobSensitivity
        {
            get => _lcdKnobSensitivity;
            set => _lcdKnobSensitivity = value;
        }
        
        public override string SettingsVersion()
        {
            return "2X";
        }

        public List<StreamDeckLayer> LayerList
        {
            get => _streamDeckLayerHandler.LayerList;
        }

        public void AddLayer(string layerName)
        {
            _streamDeckLayerHandler.AddLayer(layerName);
            SetIsDirty();
        }

        public void AddLayer(StreamDeckLayer streamDeckLayer)
        {
            _streamDeckLayerHandler.AddLayer(streamDeckLayer);
            SetIsDirty();
        }

        public void DeleteLayer(StreamDeckLayer streamDeckLayer)
        {
            _streamDeckLayerHandler.DeleteLayer(streamDeckLayer.Name);
            SetIsDirty();
        }

        public void DeleteLayer(string layerName)
        {
            _streamDeckLayerHandler.DeleteLayer(layerName);
            SetIsDirty();
        }

        public StreamDeckLayer HomeLayer
        {
            get => _streamDeckLayerHandler.HomeLayer;
        }

        public void SetHomeLayer(string layerName)
        {
            foreach (var deckLayer in _streamDeckLayerHandler.LayerList)
            {
                deckLayer.IsHomeLayer = layerName == deckLayer.Name;
            }
            SetIsDirty();
        }

        public void SetHomeLayer(StreamDeckLayer streamDeckLayer)
        {
            SetHomeLayer(streamDeckLayer.Name);
        }
    }


    public enum ControlListStreamDeck : byte
    {
        ALL,
        DCSBIOS,
        KEYS,
        BIPS
    }
}
