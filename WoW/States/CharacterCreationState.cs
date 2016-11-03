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
                if (_wowManager.CharAutoCreationFailed)
                {
                    CustomUtility.EscKeyPress(_wowManager);
                }

                //var names = CustomStateLib.GetVisibleObjectNames(_wowManager);
                //var types = CustomStateLib.GetVisibleObjectsTypeNames(_wowManager);
                //var texts = CustomStateLib.GetVisibleObjectsTexts(_wowManager);
                //var objects = CustomStateLib.GetVisibleObjects(_wowManager);

                var raceName = _wowManager.Profile.Settings.WowSettings.RaceName;
                var className = _wowManager.Profile.Settings.WowSettings.ClassName;

                var btnOkay = CustomUtility.GetVisibleButtonByName("CharCreateOkayButton", _wowManager);
                var btnRundomChar = CustomUtility.GetVisibleButtonByName("CharCreateRandomizeButton", _wowManager);

                if (btnOkay != null && _wowManager.GlueScreen == GlueScreen.CharCreate)
                {
                    CustomUtility.Sleep(TimeSpan.FromSeconds(4));

                    if (!CustomUtility.Visible("CharCreateRandomizeButton", _wowManager))
                    {
                        if (raceName != "")
                        {
                            var raceBtn = CustomUtility.findObjectByText(raceName, _wowManager);
                            raceBtn = CustomUtility.findObjectByText(raceName, _wowManager);
                            if (raceBtn != null)
                            {
                                if (!CustomUtility.ClickButton(raceBtn, _wowManager))
                                {
                                    CustomUtility.EscKeyPress(_wowManager);
                                    return;
                                } else
                                {
                                    CustomUtility.Sleep(TimeSpan.FromSeconds(4));
                                }

                                if (className != "")
                                {
                                    var classBtn = CustomUtility.findObjectByText(className, _wowManager);
                                    classBtn = CustomUtility.findObjectByText(className, _wowManager);
                                    if (classBtn != null)
                                    {
                                        if (! CustomUtility.ClickButton(classBtn, _wowManager))
                                        {
                                            CustomUtility.EscKeyPress(_wowManager);
                                            return;
                                        }

                                        CustomUtility.Sleep(TimeSpan.FromSeconds(2));
                                    } 
                                    else
                                    {
                                        _wowManager.CharAutoCreationFailed = true;
                                        _wowManager.Profile.Log("Char Class was not founded with this Name {0}", className);
                                        CustomUtility.EscKeyPress(_wowManager);
                                        return;
                                    }
                                }

                            } else
                            {
                                _wowManager.CharAutoCreationFailed = true;
                                _wowManager.Profile.Log("Char Race was not founded with this Name {0}", raceName);
                                CustomUtility.EscKeyPress(_wowManager);
                                return;
                            }
                        }

                        _wowManager.Profile.Log("Selecting char type");
                        if (! CustomUtility.ClickButton(btnOkay, _wowManager))
                        {
                            _wowManager.CharAutoCreationFailed = true;
                            _wowManager.Profile.Log("Could not click BtnOk {0}", btnOkay.Name);
                            CustomUtility.EscKeyPress(_wowManager);
                            return;
                        }
                            
                    }
                    else
                    {
                        CustomUtility.Sleep(TimeSpan.FromSeconds(2));

                        var btnDlgOkay = CustomUtility.GetVisibleButtonByName("GlueDialogButton1", _wowManager);
                        if (btnDlgOkay == null)
                        {
                            if (CustomUtility.Visible("CharacterCreateNameEdit", _wowManager))
                            {
                                _wowManager.Profile.Log("Rundomize char");
                                CustomUtility.ClickButton(btnRundomChar, _wowManager);

                                CustomUtility.Sleep(TimeSpan.FromSeconds(2));

                                CustomUtility.EnterText("CharacterCreateNameEdit", _wowManager.Profile.Settings.WowSettings.CharacterName, _wowManager);

                                CustomUtility.Sleep(TimeSpan.FromSeconds(2));

                                _wowManager.Profile.Log("Try Create Char with given Name {0}", _wowManager.Profile.Settings.WowSettings.CharacterName);
                                CustomUtility.ClickButton(btnOkay, _wowManager);

                                CustomUtility.Sleep(TimeSpan.FromSeconds(2));
                            }
                        } else
                        {
                            CustomUtility.ClickButton(btnDlgOkay, _wowManager);

                            CustomUtility.Sleep(TimeSpan.FromSeconds(2));

                            _wowManager.CharAutoCreationFailed = true;
                            _wowManager.Profile.Log("Char Already Exists with this Name {0}", _wowManager.Profile.Settings.WowSettings.CharacterName);
                            CustomUtility.EscKeyPress(_wowManager);
                            CustomUtility.EscKeyPress(_wowManager);
                            return;
                        }
                    }
                }
            }
        }
    }
}