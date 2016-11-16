﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using HighVoltz.HBRelog.FiniteStateMachine;
using HighVoltz.HBRelog.WoW.FrameXml;
using Button = HighVoltz.HBRelog.WoW.FrameXml.Button;

namespace HighVoltz.HBRelog.WoW.States
{
    internal class CharacterSelectState : State
    {
        private readonly WowManager _wowManager;

        public CharacterSelectState(WowManager wowManager)
        {
            _wowManager = wowManager;
        }

        public override int Priority
        {
            get { return 500; }
        }

        public override bool NeedToRun
        {
            get 
			{
                return (_wowManager.GameProcess != null && !_wowManager.GameProcess.HasExitedSafe()) 
				&& !_wowManager.StartupSequenceIsComplete 
				&& !_wowManager.InGame 
				&& !_wowManager.IsConnectiongOrLoading 
				&& _wowManager.GlueScreen == GlueScreen.CharSelect; 
			}
        }

        public override void Run()
        {
            if (_wowManager.Throttled)
                return;

            if (ReactivateAccountDialogVisible)
            {
                _wowManager.Profile.Log("Account has no game time, pausing.");
                _wowManager.Profile.Pause();
                return;
            }
	        
			// trial account will have a promotion frame that requires clicking a 'Play Trial' button to enter game.
			if (ClickPlayTrial())
	        {
                
                return;
	        }

            if (ShouldChangeRealm)
            {
                ChangeRealm();
                return;
            }

            var btnStarter = CustomUtility.GetVisibleButtonByName("StarterEditionPopUpExitButton", _wowManager);
            if (btnStarter != null && btnStarter.IsVisible && _wowManager.GlueScreen == GlueScreen.CharSelect)
            {
                CustomUtility.ClickButton(btnStarter, _wowManager);
            }


            HandleCharacterSelect();
        }

        private void TryCreateChar()
        {
            var btnCreateChar = CustomUtility.GetVisibleButtonByName("CharSelectCreateCharacterButton", _wowManager);

            if (btnCreateChar != null && _wowManager.GlueScreen == GlueScreen.CharSelect)
            {
                CustomUtility.SleepUntil(btnCreateChar.IsShown, TimeSpan.FromSeconds(15));

                if (!_wowManager.CharAutoCreationFailed)
                {
                    _wowManager.Profile.Log("Try to Create new Char...");
                    CustomUtility.ClickButton(btnCreateChar, _wowManager);
                }
            }
        }

        bool HandleCharacterSelect()
        {
            // CharSelectCharacterButton5ButtonTextName
            const string groupName = "ButtonTextName";
            var characterNames = (from fontString in UIObject.GetUIObjectsOfType<FontString>(_wowManager)
                                  where fontString.IsVisible && fontString.Name.Contains(groupName)
                                  let parent = fontString.Parent as Frame
                                  where parent != null
                                  let grandParent = parent.Parent as Button
                                  where grandParent != null
                                  orderby grandParent.Id
                                  select fontString.Text).ToList();

            if (!characterNames.Any())
            {
                TryCreateChar();
                return false;
            }

            var charName = _wowManager.Settings.CharacterName;
            var wantedCharIndex =
                characterNames.FindIndex(n => string.Equals(n, charName, StringComparison.InvariantCultureIgnoreCase)) + 1;

            if (wantedCharIndex == 0)
            {
	            var inactivecharName = $"{charName} |cffff2020(Inactive)|r";
	            if (characterNames.Any(n => string.Equals(inactivecharName, n, StringComparison.InvariantCultureIgnoreCase)))
	            {
					_wowManager.Profile.Status = "WoW subscription is inactive";
					_wowManager.Profile.Log("WoW subscription is inactive");
					_wowManager.Profile.Pause();
					return false;
	            }
                _wowManager.Profile.Status = $"Character name: {charName} not found. Double check spelling";
                _wowManager.Profile.Log("Character name not found. Double check spelling");

                TryCreateChar();
                return false;
            }

            // get current selected index from global variable CURRENT_SELECTED_WOW_ACCOUNT
            var currentIndex = SelectedCharacterIndex;

            if (wantedCharIndex != currentIndex)
            {
	            var index = wantedCharIndex > currentIndex
		            ? new string((char) Keys.Down, wantedCharIndex - currentIndex)
		            : new string((char) Keys.Up, currentIndex - wantedCharIndex);
	            Utility.SendBackgroundString(_wowManager.GameProcess.MainWindowHandle, index, false);
	            Utility.SleepUntil(() => SelectedCharacterIndex == wantedCharIndex, TimeSpan.FromSeconds(2));
                return false;
            }

            Utility.SendBackgroundKey(_wowManager.GameProcess.MainWindowHandle, (char)Keys.Enter, false);
            return true;
        }

        // 1-based.
        int SelectedCharacterIndex
        {
            get { return (int)_wowManager.Globals.GetValue("CharacterSelect").Table.GetValue("selectedIndex").Number; }
        }

        bool ShouldChangeRealm
        {
            get
            {
                var realmName = CurrentRealmName;
                if (string.IsNullOrEmpty(realmName))
                    return false;
                return !realmName.ToLowerInvariant().Contains(_wowManager.Settings.ServerName.ToLowerInvariant());
            }
        }

        string CurrentRealmName
        {
            get
            {
                var realmName = UIObject.GetUIObjectByName<FontString>(_wowManager, "CharSelectRealmName");
                return realmName != null ? realmName.Text.Trim() : string.Empty;
            }
        }

        readonly Stopwatch _realmChangeSw = new Stopwatch();
        void ChangeRealm()
        {
            if (_realmChangeSw.IsRunning && _realmChangeSw.Elapsed < TimeSpan.FromSeconds(5))
                return;
            var changeRealmButton = UIObject.GetUIObjectByName<Button>(_wowManager, "CharSelectChangeRealmButton");
            var clickPos = _wowManager.ConvertWidgetCenterToWin32Coord(changeRealmButton);
            Utility.LeftClickAtPos(_wowManager.GameProcess.MainWindowHandle, (int)clickPos.X, (int)clickPos.Y);
            _wowManager.Profile.Log("Changing server.");
            _realmChangeSw.Restart();
        }

	    bool ClickPlayTrial()
	    {
		    var promotionFrame = UIObject.GetUIObjectByName<Frame>(_wowManager, "PromotionFrame");
			if (promotionFrame == null || !promotionFrame.IsVisible)
				return false;

		    var playButton = promotionFrame.Children.LastOrDefault() as Button;
		    if (playButton == null)
		    {
			    Log.Write("Unable to find the 'Play Trial' button! notify developer");
				_wowManager.Profile.Pause();
				return false;
		    }
			var clickPos = _wowManager.ConvertWidgetCenterToWin32Coord(playButton);
			Utility.LeftClickAtPos(_wowManager.GameProcess.MainWindowHandle, (int)clickPos.X, (int)clickPos.Y);
			return true;
	    }


        bool ReactivateAccountDialogVisible
        {
            get
            {
                var promotionFrame = UIObject.GetUIObjectByName<Frame>(_wowManager, "ReactivateAccountDialog");
                return promotionFrame != null && promotionFrame.IsVisible;
            }
        }

        
    }
}