﻿#if FIKA_COMPAT
using Comfort.Common;
using Fika.Core.Modding;
using Fika.Core.Modding.Events;
using Fika.Core.Networking;
using Jehree.ImmersiveDaylightCycle;
using Jehree.ImmersiveDaylightCycle.FikaNetworking;
using Jehree.ImmersiveDaylightCycle.Helpers;
using LiteNetLib.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

// This class is hollowed when built as an SPT release to avoid referencing Fika while also not requiring the rest
// of the mod to check the #if FIKA_COMPAT constant

// funcs that need to always be available but hollowed in SPT release build:
    // OnHostGameStarted
    // InitOnEnable
    // InitOnDisable
    // IsFikaClient

namespace Jehree.ImmersiveDaylightCycle.FikaNetworking {
    public class DaylightSync
    {
        private NetDataWriter _writer;

        private NetDataWriter GetNetDataWriter()
        {
            if (_writer == null) {
                _writer = new NetDataWriter();
            } else {
                _writer.Reset();
            }

            return _writer;
        }

        private void OnHostGameStartedEventReceived(DaylightSyncPacket packet)
        {
            if (!Singleton<FikaClient>.Instantiated) {
                throw new Exception("Server sent OnHostGameStartedEventReceived but FikaClient is not instantiated");
            }

            Settings.SetCurrentGameTime((int)packet.hostDateTime.x, (int)packet.hostDateTime.y, (int)packet.hostDateTime.z);
            Utils.SetRaidTime(packet.hostCycleRate);
        }
        public static bool IAmFikaClient()
        {
            if (Singleton<FikaClient>.Instantiated) return true;
            return false;
        }

        public void OnHostGameStarted(Vector3 hostDateTime)
        {
            if (!Singleton<FikaServer>.Instantiated) return;

            NetDataWriter netDataWriter = GetNetDataWriter();

            DaylightSyncPacket packet = new DaylightSyncPacket { hostDateTime = hostDateTime };

            Singleton<FikaServer>.Instance.SendDataToAll(netDataWriter, ref packet, LiteNetLib.DeliveryMethod.ReliableUnordered);

            Plugin.LogSource.LogInfo("Host game ran OnHostGameStarted, packets should have sent!");
        }

        public void InitOnEnable()
        {
            FikaEventDispatcher.SubscribeEvent<FikaClientCreatedEvent>(OnFikaClientCreatedEvent);
            FikaEventDispatcher.SubscribeEvent<FikaClientDestroyedEvent>(OnFikaClientDestroyedEvent);
        }

        public void InitOnDisable()
        {
            FikaEventDispatcher.UnsubscribeEvent<FikaClientCreatedEvent>(OnFikaClientCreatedEvent);
            FikaEventDispatcher.UnsubscribeEvent<FikaClientDestroyedEvent>(OnFikaClientDestroyedEvent);
        }

        private void OnFikaClientCreatedEvent(FikaClientCreatedEvent clientCreatedEvent)
        {
            // listen for packet from server
            clientCreatedEvent.Client.packetProcessor.SubscribeNetSerializable<DaylightSyncPacket>(OnHostGameStartedEventReceived);
        }

        private void OnFikaClientDestroyedEvent(FikaClientDestroyedEvent clientDestroyedEvent)
        {
            // remove listener from client
            clientDestroyedEvent.Client.packetProcessor.RemoveSubscription<DaylightSyncPacket>();
        }

        /*
        private void OnFikaServerCreatedEvent()
        {
            //no server events currently
        }

        private void OnFikaServerDestroyedEvent()
        {
            //no server events currently
        }
        */

    }
}
#endif

