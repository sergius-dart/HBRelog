using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HighVoltz.HBRelog.FiniteStateMachine;
using HighVoltz.HBRelog.WoW.FrameXml;

namespace HighVoltz.HBRelog.WoW.States
{
    internal class CharacterCreationState : State
    {
        private readonly WowManager _wowManager;

        public CharacterCreationState(WowManager wowManager)
        {
            _wowManager = wowManager;
        }

        public override int Priority
        {
            get { return 400; }
        }

        public override bool NeedToRun
        {
	        get
	        {
                return (_wowManager.GameProcess != null && !_wowManager.GameProcess.HasExitedSafe()) 
					&& !_wowManager.StartupSequenceIsComplete && !_wowManager.InGame 
					&& !_wowManager.IsConnectiongOrLoading 
					&& _wowManager.GlueScreen == GlueScreen.CharCreate;
	        }
        }

        public override void Run()
        {
            var characterCreateFrame = UIObject.GetUIObjectByName<Frame>(_wowManager, "CharacterCreateFrame");
            if (characterCreateFrame != null && characterCreateFrame.IsVisible)
            {
                if (_wowManager.CharCreationFailed)
                {
                    CustomStateLib.EscKeyPress(_wowManager);
                }

                //var names = CustomStateLib.GetVisibleObjectNames(_wowManager);
                //var types = CustomStateLib.GetVisibleObjectsTypeNames(_wowManager);
                //var texts = CustomStateLib.GetVisibleObjectsTexts(_wowManager);
                //var objects = CustomStateLib.GetVisibleObjects(_wowManager);

                var raceName = _wowManager.Profile.Settings.WowSettings.RaceName;
                var className = _wowManager.Profile.Settings.WowSettings.ClassName;

                var btnOkay = CustomStateLib.GetVisibleButtonByName("CharCreateOkayButton", _wowManager);
                var btnRundomChar = CustomStateLib.GetVisibleButtonByName("CharCreateRandomizeButton", _wowManager);

                if (btnOkay != null && _wowManager.GlueScreen == GlueScreen.CharCreate)
                {
                    CustomStateLib.Sleep(TimeSpan.FromSeconds(4));

                    if (!CustomStateLib.Visible("CharCreateRandomizeButton", _wowManager))
                    {
                        if (raceName != "")
                        {
                            var raceBtn = CustomStateLib.findObjectByText(raceName, _wowManager);
                            raceBtn = CustomStateLib.findObjectByText(raceName, _wowManager);
                            if (raceBtn != null)
                            {
                                if (!CustomStateLib.ClickButton(raceBtn, _wowManager))
                                {
                                    CustomStateLib.EscKeyPress(_wowManager);
                                    return;
                                } else
                                {
                                    CustomStateLib.Sleep(TimeSpan.FromSeconds(4));
                                }

                                if (className != "")
                                {
                                    var classBtn = CustomStateLib.findObjectByText(className, _wowManager);
                                    classBtn = CustomStateLib.findObjectByText(className, _wowManager);
                                    if (classBtn != null)
                                    {
                                        if (! CustomStateLib.ClickButton(classBtn, _wowManager))
                                        {
                                            CustomStateLib.EscKeyPress(_wowManager);
                                            return;
                                        }

                                        CustomStateLib.Sleep(TimeSpan.FromSeconds(2));
                                    } 
                                    else
                                    {
                                        _wowManager.CharCreationFailed = true;
                                        _wowManager.Profile.Log("Char Class was not founded with this Name {0}", className);
                                        CustomStateLib.EscKeyPress(_wowManager);
                                        return;
                                    }
                                }

                            } else
                            {
                                _wowManager.CharCreationFailed = true;
                                _wowManager.Profile.Log("Char Race was not founded with this Name {0}", raceName);
                                CustomStateLib.EscKeyPress(_wowManager);
                                return;
                            }
                        }

                        _wowManager.Profile.Log("Selecting char type");
                        if (! CustomStateLib.ClickButton(btnOkay, _wowManager))
                        {
                            _wowManager.CharCreationFailed = true;
                            _wowManager.Profile.Log("Could not click BtnOk {0}", btnOkay.Name);
                            CustomStateLib.EscKeyPress(_wowManager);
                            return;
                        }
                            
                    }
                    else
                    {
                        CustomStateLib.Sleep(TimeSpan.FromSeconds(2));

                        var btnDlgOkay = CustomStateLib.GetVisibleButtonByName("GlueDialogButton1", _wowManager);
                        if (btnDlgOkay == null)
                        {
                            if (CustomStateLib.Visible("CharacterCreateNameEdit", _wowManager))
                            {
                                _wowManager.Profile.Log("Rundomize char");
                                CustomStateLib.ClickButton(btnRundomChar, _wowManager);

                                CustomStateLib.Sleep(TimeSpan.FromSeconds(2));

                                CustomStateLib.EnterText("CharacterCreateNameEdit", _wowManager.Profile.Settings.WowSettings.CharacterName, _wowManager);

                                CustomStateLib.Sleep(TimeSpan.FromSeconds(2));

                                _wowManager.Profile.Log("Try Create Char with given Name {0}", _wowManager.Profile.Settings.WowSettings.CharacterName);
                                CustomStateLib.ClickButton(btnOkay, _wowManager);

                                CustomStateLib.Sleep(TimeSpan.FromSeconds(2));
                            }
                        } else
                        {
                            CustomStateLib.ClickButton(btnDlgOkay, _wowManager);

                            CustomStateLib.Sleep(TimeSpan.FromSeconds(2));

                            _wowManager.CharCreationFailed = true;
                            _wowManager.Profile.Log("Char Already Exists with this Name {0}", _wowManager.Profile.Settings.WowSettings.CharacterName);
                            CustomStateLib.EscKeyPress(_wowManager);
                            CustomStateLib.EscKeyPress(_wowManager);
                            return;
                        }
                    }
                }
            }
        }
    }
}