/*
* Copyright (C) Lbniese 2016
* Plugin has been based on the idea of cls15's similar plugin.
* Sharing or redistributing this plugin is not permitted.
*/

using System;
using System.Diagnostics;
using System.Threading;
using Buddy.Coroutines;
using Styx;
using Styx.Common;
using Styx.CommonBot;
using Styx.CommonBot.Frames;
using Styx.Plugins;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace NoMoveDetector
{
    internal class NoMoveDetector : HBPlugin
    {
        private static Thread _restartThread;
        private WoWPoint _lastLoc;

        private Stopwatch _lastOk;
        private int _nBotRestart;

        private static void _RestartThread()
        {
            TreeRoot.Stop();
            Coroutine.Sleep(2000);
            TreeRoot.Start();
        }

        private void _OnBotStart(EventArgs args)
        {
            Logging.Write(@"[NoMoveDetector] Bot started");
            _lastLoc = StyxWoW.Me.Location;
            _lastOk.Restart();
            _restartThread = new Thread(_RestartThread);
        }

        private void _init()
        {
            Logging.Write(@"[NoMoveDetector] init");
            _lastOk = new Stopwatch();
            _nBotRestart = 0;
        }

        private void _MainPulseProc()
        {
            // Must we go futher anyway?
            if (!TreeRoot.IsRunning)
            {
                if (_lastOk.ElapsedMilliseconds > 1000*30)
                {
                    Logging.Write(@"[NoMoveDetector] LastPosition reseted, bot is not running (but pulse is called ???)");
                    _lastOk.Restart();
                }
                return;
            }

            WoWPlayer me = StyxWoW.Me;
            //Cancel timer if move > 10 yards is detected
            if (_lastLoc.Distance(me.Location) > 10f)
            {
                if (_lastOk.ElapsedMilliseconds > 1000*30)
                    Logging.Write(@"[NoMoveDetector] Move detected. LastPosition reseted");
                _lastOk.Restart();
                _lastLoc = me.Location;
                return;
            }
            /* if (LastOK.ElapsedMilliseconds > 1000 * 5 && !StyxWoW.Me.HasAura("Food"))
             { 
                 // BestFood detection correct
                 ulong nCurrentFood = CharacterSettings.Instance.DrinkName.ToUInt32();
                 WoWItem tp = ObjectManager.GetObjectByGuid<WoWItem>(nCurrentFood);
                 if (nCurrentFood == 0)
                 {

                 }
             } */
            // Have we moved whithin last 5 mins
            if (_lastOk.ElapsedMilliseconds <= 1000*60*5) return;
            if (AuctionFrame.Instance.IsVisible || MailFrame.Instance.IsVisible)
            {
                Logging.Write(@"[NoMoveDetector] not mooving last {0} min but has open frame.  LastPosition reseted",
                    _nBotRestart*5);
                _lastOk.Restart();
                _lastLoc = me.Location;
                return;
            }
            if (me.HasAura("Resurrection Sickness"))
            {
                _lastOk.Restart();
                return;
            }
            if (_nBotRestart > 1) // Not mooving for 15 min, hope you have a reloger...
            {
                Logging.Write(@"[NoMoveDetector] not mooving last 15 min : Stopping Wow...");
                Lua.DoString(@"ForceQuit()");
            }
            else
            {
                _nBotRestart++;
                Logging.Write(@"[NoMoveDetector] not mooving last {0} min : Restarting bot...", _nBotRestart*5);
                _lastOk.Restart();
                _restartThread.Start();
            }
        }

        #region Overrides of HBPlugin

        public override void OnEnable()
        {
            BotEvents.OnBotStarted += _OnBotStart;
            _init();
        }

        public override void OnDisable()
        {
            BotEvents.OnBotStarted -= _OnBotStart;
        }

        public override string ButtonText => "---";
        public override bool WantButton => false;

        public override void OnButtonPress()
        {
        }

        public override void Pulse()
        {
            _MainPulseProc();
        }

        public override string Name => "No Move Detector";
        public override string Author => "Lbniese";
        public override Version Version => new Version(1, 0, 0);

        #endregion
    }
}