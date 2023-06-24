using System.Diagnostics;
using System.Text;
using System.Xml.Linq;
using Discord;
using Discord.Commands;
using Discord.Rest;
using Newtonsoft.Json;
using uwu_mew_mew.Attributes;
using uwu_mew_mew.Bases;

namespace uwu_mew_mew.Modules;

public class DumpModule : CommandModule
{
    [PrefixCommand("dumpall")]
    public async Task DumpAllAsync(SocketCommandContext context)
    {
        if(context.User.Id != 687600977830084696)
            return;
        
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        var count = 0;

        var guild = await context.Client.Rest.GetGuildAsync(1050422060352024636);
        var channels = await guild.GetTextChannelsAsync();

        Parallel.ForEach(channels, async c => await ProcessChannel(c));

        async Task ProcessChannel(RestTextChannel channel)
        {
            try
            {
                await channel.GetMessagesAsync(1).FirstAsync();
            }
            catch
            {
                return;
            }
            
            await Task.Delay(1000);

            var path = $"data/messages_{channel.Id}.xml";
            File.Delete(path);
            await File.AppendAllTextAsync(path, $"<messages channel=\"{channel.Id}\">");

            async Task ProcessBatch(IReadOnlyCollection<IMessage> messages)
            {
                var stringBuilder = new StringBuilder();

                void ProcessMessage(IMessage currentMessage)
                {
                    try
                    {
                        var element = new XElement("message",
                            new XAttribute("content", currentMessage.Content),
                            new XAttribute("author", currentMessage.Author.Username),
                            new XAttribute("timestamp", currentMessage.Timestamp.ToString()),
                            new XAttribute("id", currentMessage.Id.ToString()),
                            new XAttribute("reactions", JsonConvert.SerializeObject(currentMessage.Reactions)));
                        if (currentMessage.Reference is not null)
                            element.Add(new XAttribute("reply",
                                currentMessage.Reference.MessageId.GetValueOrDefault(0).ToString()));
                        else
                            element.Add(new XAttribute("reply", "0"));

                        var str = element.ToString();

                        lock (stringBuilder)
                        {
                            stringBuilder.Append('\n');
                            stringBuilder.Append(' ');
                            stringBuilder.Append(' ');
                            stringBuilder.Append(str);
                        }

                        count++;
                    }
                    catch
                    {
                        //whatever i dont care
                    }
                }

                Parallel.ForEach(messages, ProcessMessage);

                lock (path)
                {
                    File.AppendAllText(path, stringBuilder.ToString());
                }

                Console.Write($"| {count,7} | {MathF.Round(count / (stopwatch.ElapsedMilliseconds / 1000), 2),5} msg/s | {MathF.Round(stopwatch.ElapsedMilliseconds / 1000),3} s |\r");
            }
            
            var messagesAsync = channel.GetMessagesAsync(int.MaxValue);

            await foreach (var messages in messagesAsync) ProcessBatch(messages);

            await File.AppendAllTextAsync(path, "\n</messages>");
        }

        Console.WriteLine("\n\nDone in " + stopwatch.Elapsed);
    }
}