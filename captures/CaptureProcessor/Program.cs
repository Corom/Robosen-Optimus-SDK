
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using Robosen.Optimus.Protocol;
using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;

var packets = PacketReader.ReadAllPackets(@"..\..\..\..\files");


//foreach (var packet in packets.Where(p => p.IsRobotCommunication && (p.Command == CommandType.ExitEditor || p.Command == CommandType.States)))
//{
//    Console.WriteLine($"{packet.FrameNumber} - {packet.Time} - {packet.IsMaster} - {packet.Operation} - {packet.Command} - {packet.Data}");
//}


var stats = new ConcurrentDictionary<CommandType, CommandStats>();

CommandStats? lastCommand = null;
foreach (var packet in packets.Where(p => p.IsRobotCommunication))
{
    if (lastCommand == null || packet.IsMaster)
    {
        lastCommand = stats.GetOrAdd(packet.Command, c => new CommandStats(c));
        lastCommand.Requests.Add(packet.Data);

        // reuse the session when we send the same command twice in a row without a response since this can be because of a resent message
        if (lastCommand.CurrentSession != null && lastCommand.CurrentSession.Request == packet.Data && lastCommand.CurrentSession.Reponses.Count == 0)
        {
            if (string.IsNullOrWhiteSpace(lastCommand.CurrentSession.Comment))
                lastCommand.CurrentSession.Comment = packet.comment;
        }
        else
        {
            lastCommand.CurrentSession = new Session(packet);
            lastCommand.Sessions.Add(lastCommand.CurrentSession);
        }
        
    }
    else
    {
        lastCommand.Responses.Add(packet.Data);
        lastCommand.ResponsesTypes.Add(packet.Command);
        
        // dont add duplicate responses to the 
        if (lastCommand.CurrentSession.Reponses.LastOrDefault() != packet.Data)
            lastCommand.CurrentSession.Reponses.Add(packet.Data);
        lastCommand.CurrentSession.Elapsed = packet.Time - lastCommand.CurrentSession.Start;
    }
}


foreach (var (cmd,stat) in stats.Where(s => s.Key != CommandType.Invalid))
{
    Console.WriteLine($"{cmd} - {stat.Requests.Count} - {stat.Responses.Count} - ({string.Join(", ", stat.ResponsesTypes)})");
    foreach (var session in stat.UniqueSessions)
    {
        Console.WriteLine($"    {session.Comment,20}: {session.Elapsed: 0.000} :{ session.Request } - {string.Join(", ", session.Reponses)}  {session.File}:{session.FrameNumber}");
    }
    Console.WriteLine();
}



Console.WriteLine("done");


class CommandStats
{
    public CommandStats(CommandType command)
    {
        Command = command;
    }

    CommandType Command { get; }

    public HashSet<string> Requests { get; } = new HashSet<string>();
    public HashSet<string> Responses { get; } = new HashSet<string>();
    public HashSet<CommandType> ResponsesTypes { get; } = new HashSet<CommandType>();
    public List<Session> Sessions { get; } = new List<Session>();

    // give comments precident over non comments, but preserve order
    public IEnumerable<Session> UniqueSessions => Sessions
        .Where(s => !string.IsNullOrWhiteSpace(s.Comment))
        .Concat(Sessions.Where(s => string.IsNullOrWhiteSpace(s.Comment)))
        .DistinctBy(s => s.Request + string.Concat(s.Reponses));

    public Session CurrentSession { get; set; }
}

class Session
{

    public Session(Packet packet)
    {
        Request = packet.Data;
        Comment = packet.comment;
        Start = packet.Time;
        File = packet.File;
        FrameNumber = packet.FrameNumber;
    }

    public string File { get; }
    public int FrameNumber { get; }

    public string Request { get; }
    public List<string> Reponses { get; } = new List<string>();
    public string Comment { get; set; }
    public double Start { get; }
    public double Elapsed { get; set; }
}


class Packet
{
    [Name("No.")]
    public int FrameNumber { get; set; }
    public double Time { get; set; }
    public string Source { get; set; }
    public string Info { get; set; }
    public string comment { get; set; }

    [Ignore] public string File { get; set; }
    [Ignore] public string Data { get; set; }

    public bool IsMaster => Source.StartsWith("Master");
    public string Operation => Info.Split('-', ',')[0].TrimEnd();
    public bool IsRobotCommunication => (Operation == "Sent Write Request" || Operation == "Rcvd Handle Value Notification") && !Info.Contains("Client Characteristic Configuration");
    public bool HasData => Data.Length > 0;
    public bool IsTruncated => Data.Contains("\\");
    public CommandType Command => HasData && Data.Length > 6 ? (CommandType)Convert.ToByte(Data.Substring(6, 2), 16) : CommandType.Invalid;
    

    public DataPacket? ParseData() => HasData ? new DataPacket(Data) : null;


}

static class PacketReader
{

    public static IEnumerable<Packet> ReadAllPackets(string directoryPath)
    {
        var files = Directory.EnumerateFiles(directoryPath, "*.csv").OrderBy(f => f);
        foreach (var file in files)
        { 
            Console.WriteLine(Path.GetFileName(file));
            var frameData = GetFrameData(file.Replace(".csv", ".json"));
            using (var reader = new StreamReader(file))
            {
                var csvReader = new CsvReader(reader, new CsvConfiguration(CultureInfo.CurrentCulture));
                foreach (var packet in csvReader.GetRecords<Packet>())
                {
                    // alwasys get the data from the json file because it will be truncated in the CSV
                    packet.Data = frameData.GetValueOrDefault(packet.FrameNumber) ?? string.Empty;
                    packet.File = Path.GetFileName(file);
                    yield return packet;
                }
            }
        }
    }

    private static Dictionary<int, string> GetFrameData(string filename)
    {
        var data = new Dictionary<int, string>();
        var reader = new Utf8JsonReader(File.ReadAllBytes(filename));
        var currentFrame = 0;
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var name = reader.GetString();
                if (name == "frame.number")
                {
                    reader.Read();
                    currentFrame = int.Parse(reader.GetString());
                }
                if (name == "btatt.value")
                {
                    reader.Read();
                    data.Add(currentFrame, reader.GetString().Replace(":", ""));
                }
            }
        }
        return data;
    }
}





