using HighVoltz.HBRelog.WoW.FrameXml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HighVoltz.HBRelog.WoW.States
{
    class CustomStateLib
    {

        public static bool EnterText(string editBoxName, string text, WowManager _wowManager)
        {
            var editBox = UIObject.GetUIObjectByName<EditBox>(_wowManager, editBoxName);
            if (editBox == null || !editBox.IsVisible || !editBox.IsEnabled)
                return false;

            var editBoxText = editBox.Text;
            if (editBox.MaxLetters > 0 && text.Length > editBox.MaxLetters)
                text = text.Substring(0, editBox.MaxLetters);

            if (string.Equals(editBoxText, text, StringComparison.InvariantCultureIgnoreCase))
                return false;

            // do we have focus?
            if (!editBox.HasFocus)
            {
                Utility.SendBackgroundString(_wowManager.GameProcess.MainWindowHandle, "\t", false);
                _wowManager.Profile.Log("Pressing 'tab' key to gain set focus to {0}", editBoxName);
                Utility.SleepUntil(() => editBox.HasFocus, TimeSpan.FromSeconds(2));
                return true;
            }
            // check if we need to remove exisiting text.
            if (!string.IsNullOrEmpty(editBoxText))
            {
                Utility.SendBackgroundKey(_wowManager.GameProcess.MainWindowHandle, (char)System.Windows.Forms.Keys.End, false);
                Utility.SendBackgroundString(_wowManager.GameProcess.MainWindowHandle, new string('\b', editBoxText.Length * 2), false);
                _wowManager.Profile.Log("Pressing 'end' + delete keys to remove contents from {0}", editBoxName);
            }
            Utility.SendBackgroundString(_wowManager.GameProcess.MainWindowHandle, text);
            _wowManager.Profile.Log("Sending {0}letters to {1}", editBox.IsPassword ? "" : text.Length + " ", editBoxName);

            TimeSpan.FromSeconds(2);
            return true;
        }

        public static bool Sleep(TimeSpan maxSleepTime)
        {
            var sleepStart = DateTime.Now;
            var timeOut = false;
            while ((timeOut = DateTime.Now - sleepStart >= maxSleepTime) == false)
                Thread.Sleep(10);
            return !timeOut;
        }

        public static void EscKeyPress(WowManager _wowManager)
        {
            Utility.SendBackgroundKey(_wowManager.GameProcess.MainWindowHandle, (char)System.Windows.Forms.Keys.Escape, false);
            _wowManager.Profile.Log("Pressing 'Esc' key to exit character creation screen");
            TimeSpan.FromSeconds(1);
        }

        public static bool Visible(string name, WowManager _wowManager)
        {
            var names = (from obj in UIObject.GetUIObjectsOfType<Frame>(_wowManager)
                    where obj.IsVisible
                    select obj.Name).ToList();

            return names.Find(n => n == name) != null;
        }

        public static bool Visible(Frame frame, WowManager _wowManager)
        {
            var names = (from obj in UIObject.GetUIObjectsOfType<Frame>(_wowManager)
                         where obj.IsVisible
                         select obj).ToList();

            return names.Find(n => n == frame) != null;
        }

        public static List<string> GetVisibleObjectNames(WowManager _wowManager)
        {
            return (from obj in UIObject.GetUIObjectsOfType<Frame>(_wowManager)
                    where obj.IsVisible
                    select obj.Name).ToList();
        }

        public static List<string> GetVisibleObjectsTexts(WowManager _wowManager)
        {
            return (from obj in UIObject.GetUIObjectsOfType<Frame>(_wowManager)
                    where obj.IsVisible
                    select obj.Regions.OfType<FontString>().FirstOrDefault()?.Text ?? "").ToList();
        }

        public static List<string> GetVisibleObjectsTypeNames(WowManager _wowManager)
        {
            return (from obj in UIObject.GetUIObjectsOfType<Frame>(_wowManager)
                    where obj.IsVisible
                    select obj.GetType().ToString()).ToList();
        }

        public static List<Frame> GetVisibleObjects(WowManager _wowManager)
        {
            return (from obj in UIObject.GetUIObjectsOfType<Frame>(_wowManager)
                    where obj.IsVisible
                    select obj).ToList();
        }

        public static string GetObjectText(string name, WowManager _wowManager)
        {
            var obj = UIObject.GetUIObjectByName<Frame>(_wowManager, name);

            if (obj != null && obj.IsVisible)
                return obj.Regions.OfType<FontString>().FirstOrDefault()?.Text ?? "";
            return "";
        }

        public static Frame findObjectByText(string text, WowManager _wowManager)
        {
            var objects = GetVisibleObjects(_wowManager);
            foreach (var obj in objects)
            {
                if (GetObjectText(obj.Name, _wowManager) == text)
                {
                    return obj;
                }
            }
            return null;
        }

        public static List<string> GetVisibleButtonNames(WowManager _wowManager)
        {
            return (from button in UIObject.GetUIObjectsOfType<Button>(_wowManager)
                            where button.IsVisible
                            select button.Name).ToList();
        }

        public static Button GetVisibleButtonByName(string name, WowManager _wowManager)
        {
            var button = UIObject.GetUIObjectByName<Button>(_wowManager, name);
            if (button != null && button.IsVisible)
            {
                return button;
            }
            return null;
        }

        public static bool ClickButton(Frame obj, WowManager _wowManager)
        {
            if (obj != null)
            {
                var clickPos = _wowManager.ConvertWidgetCenterToWin32Coord(obj);
                Utility.LeftClickAtPos(_wowManager.GameProcess.MainWindowHandle, (int)clickPos.X, (int)clickPos.Y);
                TimeSpan.FromSeconds(2);
                return true;
            }
            return false;
        }

    }
}
