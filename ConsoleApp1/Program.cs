﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ConsoleApp1.Mw2;
using ConsoleApp1.Mw2.Secret;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using Discord.Net;
using System.Linq;

namespace SoloLearn
{
    class Program
    {
        public static Task Main() => new Program().MainAsync();
        private DiscordSocketClient _client;
        private List<String> _mapList = new List<string>();
        private String _lastMap = "";
        private readonly Dictionary<ulong, int> _vetoList = new Dictionary<ulong, int>();
        private  Aufsatzfactory _aufsätze;
        private  Waffenfactory _waffen;

        public async Task MainAsync()
        {
            try
            {
                _mapList = InitiateMaps();

                _aufsätze = new Aufsatzfactory();

                _waffen = new Waffenfactory(_aufsätze);

                //Schulproxy umgehen
                //var webproxy = new System.Net.WebProxy("192.168.10.10", 3128);
                //webproxy.UseDefaultCredentials = true;
                //var config = new DiscordSocketConfig
                //{
                //    WebSocketProvider = DefaultWebSocketProvider.Create(proxy: webproxy),
                //    RestClientProvider = DefaultRestClientProvider.Create(useProxy: true),
                //};
                //_client = new DiscordSocketClient(config);

                _client = new DiscordSocketClient();
                _client.Log += Log;
                var token = Token.GetToken();
                await _client.LoginAsync(TokenType.Bot, token);
                await _client.StartAsync();
                _client.SlashCommandExecuted += SlashCommandHandler;

                _client.Ready += Client_Ready;
                // Block this task until the program is closed.
                await Task.Delay(-1);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Console.ReadKey();
            }
        }

        private async Task SlashCommandHandler(SocketSlashCommand command)
        {
            if (command.Data.Name == "random-loadout")
            {
                String output = "";
                Loadout loudout = new Loadout(_waffen);

                if (loudout.Perks.Perk1 == "Aufsatz Pro")
                {
                    output += ("Primary: " + loudout.Primary_waffe.Name);
                    if (loudout.Primary_waffe.Aufsätze != null)
                    {
                        Aufsatz first_Aufsatz = loudout.Primary_waffe.GetRandomAufsatz();
                        output += " | Aufsatz: " + first_Aufsatz.Name + " + " + loudout.Primary_waffe.GetRandomAufsatz(first_Aufsatz).Name + "\n";
                    }
                    else
                    {
                        output += "\n";
                    }

                    output += "Secondary: " + loudout.Secondary_waffe.Name;
                    if (loudout.Secondary_waffe.Aufsätze != null)
                    {
                        output += " | Aufsatz: " + loudout.Secondary_waffe.GetRandomAufsatz().Name + "\n";
                    }
                    else
                    {
                        output += "\n";
                    }
                }
                else
                {
                    output += "Primary: " + loudout.Primary_waffe.Name;
                    if (loudout.Primary_waffe.Aufsätze != null)
                    {
                        output += " | Aufsatz: " + loudout.Primary_waffe.GetRandomAufsatz().Name + "\n";
                    }

                    output += "Secondary: " + loudout.Secondary_waffe.Name;
                    if (loudout.Secondary_waffe.Aufsätze != null)
                    {
                        output += " | Aufsatz: " + loudout.Secondary_waffe.GetRandomAufsatz().Name + "\n";
                    }
                    else
                    {
                        output += "\n";
                    }

                }

                output += "Perk1: " + loudout.Perks.Perk1 + "\n";

                output += "Perk2: " + loudout.Perks.Perk2 + "\n";

                output += "Perk3: " + loudout.Perks.Perk3 + "\n";

                output += "Ausrüstung: " + loudout.Ausrüstung.Name + "\n";

                output += "Spezialgranate: " + loudout.Spezialgranate.Name + "\n";

                output += "Todesserie: " + loudout.Todesserie.Name;

                await command.RespondAsync(output);

            }
            else if (command.Data.Name == "random-map")
            {
                await command.RespondAsync(Random_Map(_mapList));
            }
            else if (command.Data.Name == "veto")
            {
                //Check if User is allowed to veto
                ulong user = command.User.Id;
                int day = DateTime.Now.Day;

                if (!_vetoList.ContainsKey(user))
                {
                    _vetoList.Add(user, day);
                    _mapList.Remove(_lastMap);
                    await command.RespondAsync(Random_Map(_mapList));
                }

                if (_vetoList.TryGetValue(user, out int listedDay))
                {
                    if (listedDay == day)
                    {
                        // List of insults
                        await command.RespondAsync("Not allowed to veto anymore");

                    }
                    else
                    {
                        _mapList.Remove(_lastMap);
                        await command.RespondAsync(Random_Map(_mapList));
                    }
                }
            }
            else if (command.Data.Name == "ini_maps")
            {
                _mapList = InitiateMaps();
                await command.RespondAsync($"Load all maps into List");
            }
            else if (command.Data.Name == "aufsatz-ban")
            {
                Aufsatz remove = new Aufsatz(command.Data.Options.First().Value.ToString());
                _waffen.Primär_Waffen_Liste = _waffen.RemoveAufsätze(remove, _waffen.Primär_Waffen_Liste);
                _waffen.Secondary_Waffen_Liste = _waffen.RemoveAufsätze(remove, _waffen.Secondary_Waffen_Liste);

                await command.RespondAsync($"{remove.Name} removed");
            }
            else
            {
                await command.RespondAsync($"{command.Data.Name} is not yet implemented");
            }

        }

        public async Task Client_Ready()
        {
            // Let's build a guild command! We're going to need a guild so lets just put that in a variable.
            var guild = _client.GetGuild(254587476503429120);
            // Next, lets create our slash command builder. This is like the embed builder but for slash commands.
            // Note: Names have to be all lowercase and match the regular expression ^[\w-]{3,32}$

            var random_map = new SlashCommandBuilder();
            random_map.WithName("random-map");
            random_map.WithDescription("Random Mw2 Map");

            var random_loadout = new SlashCommandBuilder();
            random_loadout.WithName("random-loadout");
            random_loadout.WithDescription("Random Mw2 Loadout!");

            var veto_map = new SlashCommandBuilder();
            veto_map.WithName("veto");
            veto_map.WithDescription("Veto the last map!");

            var initiate_maps = new SlashCommandBuilder();
            initiate_maps.WithName("ini_maps");
            initiate_maps.WithDescription("Restart Map List");

            var ban_aufsatz = new SlashCommandBuilder()
                .WithName("aufsatz-ban")
                .WithDescription("Ban aufsatz from weapons")
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("aufsatz")
                    .WithDescription("Ban the selected Aufsatz")
                    .AddChoice("Herzschlagsensor", "Herzschlagsensor")
                    .AddChoice("Thermal", "Thermal")
                    .WithType(ApplicationCommandOptionType.String)
            );
            

            try
            {
                await guild.DeleteApplicationCommandsAsync();
                await _client.BulkOverwriteGlobalApplicationCommandsAsync(new Discord.ApplicationCommandProperties[] {});
                // Now that we have our builder, we can call the CreateApplicationCommandAsync method to make our slash command.
                await guild.CreateApplicationCommandAsync(random_loadout.Build());
                await guild.CreateApplicationCommandAsync(random_map.Build());
                await guild.CreateApplicationCommandAsync(veto_map.Build());
                await guild.CreateApplicationCommandAsync(initiate_maps.Build());
                await guild.CreateApplicationCommandAsync(ban_aufsatz.Build());

            }
            catch (HttpException exception)
            {
                // If our command was invalid, we should catch an ApplicationCommandException. This exception contains the path of the error as well as the error message. You can serialize the Error field in the exception to get a visual of where your error is.
                var json = JsonConvert.SerializeObject(exception.Errors, Formatting.Indented);

                // You can send this error somewhere or just print it to the console, for this example we're just going to print it.
                Console.WriteLine(json);
            }
        }
        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        public List<String> InitiateMaps()
        {
            List<String> map_List = new List<string>
            {
                "Afghan",
                "Bailout",
                "Carnival",
                "Derail",
                "Estate",
                "Favela",
                "Fuel",
                "Highrise",
                "Invasion",
                "Karachi",
                "Overgrown",
                "Quarry",
                "Rundown",
                "Salvage",
                "Scrapyard",
                "Skidrow",
                "Storm",
                "Strike",
                "Sub Base",
                "Terminal",
                "Trailer Park",
                "Underpass",
                "Vacant "
            };
            return map_List;
        }

        public String Random_Map(List<String> map_List)
        {
            int size = map_List.Count;
            _lastMap = map_List[StaticRandom.Instance.Next(0, size - 1)];
            return _lastMap;
        }
    }
}