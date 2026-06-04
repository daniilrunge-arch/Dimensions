using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Terraria;
using TShockAPI;
using CaxarokLink.Models;
using CaxarokLink.Config;

namespace CaxarokLink
{
    public class CaxarokLinkPlugin : TerrariaPlugin
    {
        public override string Name => "CaxarokLink";
        public override Version Version => new Version(1, 0, 0);
        public override string Author => "nana.c";
        public override string Description => "Плагин для объединения серверов caxarok.ru";

        private ServerConfig _config;
        private HttpClient _httpClient;

        public CaxarokLinkPlugin(Main game) : base(game)
        {
            _httpClient = new HttpClient();
        }

        public override void Initialize()
        {
            Commands.RegisterCommand("ss.join", JoinServerCommand);
            Commands.RegisterCommand("ss.who", WhoCommand);
            Commands.RegisterCommand("ss.online", OnlineCommand);
            Commands.RegisterCommand("ss.w", WhisperCommand);
            Commands.RegisterCommand("ss.cmd", RemoteCommand);

            LoadConfig();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Commands.UnregisterCommand("ss.join");
                Commands.UnregisterCommand("ss.who");
                Commands.UnregisterCommand("ss.online");
                Commands.UnregisterCommand("ss.w");
                Commands.UnregisterCommand("ss.cmd");
                _httpClient?.Dispose();
            }
            base.Dispose(disposing);
        }

        private void LoadConfig()
        {
            string configPath = Path.Combine(TShock.SavePath, "caxaroklink.json");

            if (!File.Exists(configPath))
            {
                CreateDefaultConfig(configPath);
            }

            string configJson = File.ReadAllText(configPath);
            _config = JsonSerializer.Deserialize<ServerConfig>(configJson);
        }

        private void CreateDefaultConfig(string path)
        {
            var defaultConfig = new ServerConfig
            {
                CurrentServerName = "Lobby",
                SecretKey = "your-super-secret-key-lobby",
                Servers = new List<ServerInfo>
                {
                    new ServerInfo
                    {
                        Name = "Lobby",
                        Address = "127.0.0.1",
                        Port = 7777,
                        SecretKey = "your-super-secret-key-lobby"
                    },
                    new ServerInfo
            {
                Name = "Survival",
                Address = "127.0.0.1",
                Port = 7778,
                SecretKey = "your-super-secret-key-survival"
            },
            new ServerInfo
            {
                Name = "PvP",
                Address = "127.0.0.1",
                Port = 7779,
                SecretKey = "your-super-secret-key-pvp"
            }
        }
    };

    string json = JsonSerializer.Serialize(defaultConfig, new JsonSerializerOptions { WriteIndented = true });
    File.WriteAllText(path, json);
}

private async void JoinServerCommand(CommandArgs args)
{
    if (args.Parameters.Count < 1)
    {
        args.Player.SendMessage("Использование: /join <server_name>", Color.Yellow);
        return;
    }

    string targetServerName = args.Parameters[0];
    var targetServer = _config.Servers.Find(s =>
        s.Name.Equals(targetServerName, StringComparison.OrdinalIgnoreCase));

    if (targetServer == null)
    {
        args.Player.SendMessage($"Сервер '{targetServerName}' не найден.", Color.Red);
        return;
    }

    // Сохраняем состояние игрока
    var playerData = new PlayerData
    {
        PlayerName = args.Player.Name,
        PositionX = args.Player.X,
        PositionY = args.Player.Y,
        Health = args.Player.TPlayer.statLife,
        Mana = args.Player.TPlayer.statMana,
        SourceServer = _config.CurrentServerName
    };

    // Генерируем JWT‑токен
    string token = GenerateJwtToken();

    // Отправляем запрос на целевой сервер
    var request = new HttpRequestMessage(HttpMethod.Post,
        $"http://{targetServer.Address}:{targetServer.Port}/api/transfer");
    request.Headers.Add("Authorization", $"Bearer {token}");
    request.Content = new StringContent(JsonSerializer.Serialize(playerData),
        null, "application/json");

    try
    {
        var response = await _httpClient.SendAsync(request);

        if (response.IsSuccessStatusCode)
        {
            args.Player.SendMessage($"Перемещаемся на сервер {targetServerName}...", Color.Green);
            args.Player.Disconnect("Transferring to another server...");
        }
        else
        {
            args.Player.SendMessage("Ошибка при подключении к серверу.", Color.Red);
        }
    }
    catch (Exception ex)
    {
        args.Player.SendMessage($"Ошибка сети: {ex.Message}", Color.Red);
    }
}

private async void WhoCommand(CommandArgs args)
{
    // Онлайн на текущем сервере
    int localOnline = TShock.Players.Count(p => p != null && p.IsActive);
    args.Player.SendMessage($"Онлайн на этом сервере: {localOnline}", Color.Cyan);

    // Онлайн на других серверах
    foreach (var server in _config.Servers)
    {
        if (server.Name != _config.CurrentServerName)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get,
                    $"http://{server.Address}:{server.Port}/api/online");
                request.Headers.Add("Authorization", $"Bearer {GenerateJwtToken()}");

                var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var onlineData = JsonSerializer.Deserialize<OnlineResponse>(content);
                    args.Player.SendMessage($"Сервер {onlineData.ServerName}: {onlineData.OnlineCount} игроков", Color.Cyan);
                }
            }
            catch
            {
                args.Player.SendMessage($"Сервер {server.Name}: недоступен", Color.Gray);
            }
        }
    }
}

private void OnlineCommand(CommandArgs args)
{
    WhoCommand(args); // Используем ту же логику
}

private async void WhisperCommand(CommandArgs args)
{
    if (args.Parameters.Count < 2)
    {
        args.Player.SendMessage("Использование: /
