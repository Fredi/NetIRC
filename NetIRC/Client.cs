﻿using NetIRC.Connection;
using NetIRC.Messages;
using System;
using System.Threading.Tasks;

namespace NetIRC
{
    public class Client : IDisposable
    {
        private readonly IConnection connection;

        public event IRCRawDataHandler OnRawDataReceived;
        public event ParsedIRCMessageHandler OnIRCMessageParsed;

        public EventHub EventHub { get; }

        public Client(IConnection connection)
        {
            this.connection = connection;
            this.connection.DataReceived += Connection_DataReceived;

            EventHub = new EventHub(this);
            InitializeDefaultEventHubEvents();
        }

        private void InitializeDefaultEventHubEvents()
        {
            EventHub.Ping += EventHub_Ping;
        }

        private async void EventHub_Ping(object sender, IRCMessageEventArgs<PingMessage> e)
        {
            await SendAsync(new PongMessage(e.IRCMessage.Target));
        }

        private void Connection_DataReceived(object sender, DataReceivedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Data))
            {
                return;
            }

            var rawData = e.Data;

            OnRawDataReceived?.Invoke(this, e.Data);

            var parsedIRCMessage = new ParsedIRCMessage(rawData);

            OnIRCMessageParsed?.Invoke(this, parsedIRCMessage);

            var serverMessage = IRCMessage.Create(parsedIRCMessage);

            serverMessage?.TriggerEvent(EventHub);
        }

        public async Task ConnectAsync(string host, int port, string nick, string realName)
        {
            await connection.ConnectAsync(host, port);

            await SendAsync(new NickMessage(nick));
            await SendAsync(new UserMessage(nick, realName));
        }

        public async Task SendRaw(string rawData)
        {
            await connection.SendAsync(rawData);
        }

        public async Task SendAsync(IClientMessage message)
        {
            await connection.SendAsync(message.ToString());
        }

        public void Dispose()
        {
            connection.Dispose();
        }
    }
}
