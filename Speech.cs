using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;

namespace DibbrBot;

public class Speech
{
    AudioFileReader _audioFile;
    IWavePlayer _player = new WasapiOut();

    public async Task Speak(string txt, int voiceIndex = 0)
    {
        var voices = new[] {"Brian", "Joey", "Amy", "Emma", "Nicole", "Justin", "Ivy", "Mizuki (Japanese)"};
        var client = new HttpClient();
        var content = new StringContent($"{{\"text\":\"{txt}\",\"voice\":\"{voices[voiceIndex]}\"}}", Encoding.UTF8,
            "application/json");
        content.Headers.ContentType = new("application/json");
        var response = await client.PostAsync(
            "https://us-central1-sunlit-context-217400.cloudfunctions.net/streamlabs-tts", content);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        if (!body.Contains("http")) return;

        body = body[body.IndexOf("http")..];
        body = body[..body.IndexOf("\"")];


        while (_player.PlaybackState == PlaybackState.Playing) await Task.Delay(1);


        var mf = new MediaFoundationReader(body);
        _player = new WasapiOut();

        _player.Init(mf);
        _player.Play();
    }
}
/*async Task Speak(string txt)
{
    if
      (silent) return; await SL(txt);
    await SpeakDialog(txt);
    return;
    try
    {
        await Task.Delay(1);
        SpeechOptions options = null;
        ctx = new CancellationTokenSource();
        await TextToSpeech.SpeakAsync(txt, options, ctx.Token);
    }
    catch (Exception e)
    {
        a = e.ToString();
    }
}*/

public class SpeechCap
{
    static Timer _timer;
    static int _c = 10;
    string[] _answer;
    public string OutputFilename = "demo2.wav";
    public string Text;

    WaveInEvent _waveInEvent;

    //WaveIn waveIn;
    WaveFileWriter _writer;

    public SpeechCap() => OutputFilename = _c++ + ".wav";

    void waveIn_DataAvailable(object sender, WaveInEventArgs e) { _writer.Write(e.Buffer, 0, e.BytesRecorded); }

    void waveIn_RecordingStopped(object sender, EventArgs e)
    {
        _waveInEvent.Dispose();
        _waveInEvent = null;
        //writer.Flush();       
        _writer.Close();
        _writer = null;
    }

    public void Listen()
    {
        _waveInEvent = new();
        _waveInEvent.DeviceNumber = 0;
        _waveInEvent.DataAvailable += waveIn_DataAvailable;
        _waveInEvent.RecordingStopped += waveIn_RecordingStopped;
        _waveInEvent.WaveFormat = new(16000, 1);
        _writer = new(OutputFilename, _waveInEvent.WaveFormat);
        _waveInEvent.StartRecording();
    }

    public void StopListen()
    {
        if (_waveInEvent != null) _waveInEvent.StopRecording();

        if (_writer != null)
        {
            _writer.Flush();
            _writer.Dispose();
        }

        WorkWithApi();
    }

    public async void WorkWithApi()
    {
        byte[] byteArray = null;
        while (byteArray == null)
        {
            try { byteArray = File.ReadAllBytes(OutputFilename); }
            catch (Exception e) { Console.WriteLine("failed to read file"); }
        }

        Console.WriteLine("read " + byteArray.Length + " bytes");

        var client = new HttpClient();
        client.BaseAddress = new("https://www.google.com");
        client.DefaultRequestHeaders.Accept.Add(new("application/json")); //ACCEPT header

        //  HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "/speech-api/v2/recognize?output=json&lang=en-US&key=AIzaSyBOti4mM-6x9WDnZIjIeyEU21OpBXqWBgw");
        //  request.Content = new ByteArrayContent(byteArray);
        // request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("audio/wav");

        var byteArrayContent = new ByteArrayContent(byteArray);
        byteArrayContent.Headers.ContentType = MediaTypeHeaderValue.Parse("audio/l16; rate=16000");
        // Wrap/encode the content as "multipart/form-data"
        // See example of how the output/request looks here:
        // https://dotnetfiddle.net/qDMwFh
        var requestContent = new MultipartFormDataContent {{byteArrayContent, "audio", "filename.wav"}};

        var result = await client
            .PostAsync("/speech-api/v2/recognize?output=json&lang=en-US&key=AIzaSyBOti4mM-6x9WDnZIjIeyEU21OpBXqWBgw",
                requestContent).ConfigureAwait(false);
        var stream = await result.Content.ReadAsStreamAsync();

        // var json = await result.Content.ReadAsStringAsync();

        var reader = new StreamReader(stream);
        var freq = reader.ReadToEnd().ToLower();
        var speech = "";
        var speech2 = "";
        var answer = freq.Split('"');
        for (var i = 0; i < answer.Length; i++)
        {
            if (answer[i] != ":") continue;

            if (speech == "")
                speech = answer[i + 1];
            else if (speech2 == "") speech2 = answer[i + 1];
        }

        Console.WriteLine(speech + freq);
        if (speech.Length > 0) Assistant2.Text += speech;

        reader.Close();
        // response.Close();
        //Console.WriteLine("Response: {0}", response.Result);


        // client.        
        // HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://www.google.com/speech-api/v2/recognize?output=json&lang=en-US&key=AIzaSyBOti4mM-6x9WDnZIjIeyEU21OpBXqWBgw");
        // https://speech.googleapis.com/v1p1beta1/speech:recognize?key=AIzaSyBOti4mM-6x9WDnZIjIeyEU21OpBXqWBgw");
        //"https://www.google.com/speech-api/v2/recognize?output=json&lang=en-US&key=AIzaSyBOti4mM-6x9WDnZIjIeyEU21OpBXqWBgw");     
        //request.Method = "POST";
        //request.AutomaticDecompression = DecompressionMethods.GZip;
        /*
         request.ContentType = "audio/l16; rate=16000";
         //"16000";    
         request.ContentLength = byteArray.Length;
         request.GetRequestStream().Write(byteArray, 0, byteArray.Length);
         */
    }
}

public static class Assistant2
{
    public static string Text;

    // static WaveIn waveIn;
    static WaveFileWriter _writer;
    static string _outputFilename = "demo2.wav";
    static string[] _answer;
    static Timer _timer;
    static WaveInEvent _waveInEvent;
    static readonly List<SpeechCap> Caps = new();
    static bool _re;

    public static void Start() { _timer = new(on_timer, 0, 0, 3000); }

    static void on_timer(object? state)
    {
        if (_re) return;

        _re = true;
        if (Caps.Count > 0)
        {
            Caps.Last().StopListen();
            Thread.Sleep(1000);
        }

        var cap = new SpeechCap();
        cap.Listen();
        Caps.Add(cap);
        _re = false;
    }
}