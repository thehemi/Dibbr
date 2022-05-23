using System.IO;
using System.Net;
using System.Collections.Generic;
using NAudio.Wave;
using System.Net;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Net.Http.Headers;
using System;
using NAudio.Wave;
using NAudio.FileFormats;
using NAudio.CoreAudioApi;
using NAudio;
using System.Threading.Tasks;
using System.Text;

namespace DibbrBot;

public class Speech
{
    IWavePlayer player = new WasapiOut();
    AudioFileReader audioFile;
    
    public async Task Speak(string txt, int voiceIndex = 0)
    {

        var voices = new
            [] { "Brian", "Joey", "Amy", "Emma", "Nicole", "Justin", "Ivy", "Mizuki (Japanese)" };
        var client = new HttpClient();
        var content = new StringContent($"{{\"text\":\"{txt}\",\"voice\":\"{voices[voiceIndex]}\"}}", Encoding.UTF8, "application/json");
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        var response = await client.PostAsync(
            "https://us-central1-sunlit-context-217400.cloudfunctions.net/streamlabs-tts", content);
        response.EnsureSuccessStatusCode();
        string body = await response.Content.ReadAsStringAsync();
        if
          (!body.Contains("http")) return;
        body = body.Substring(body.IndexOf("http"));
        body = body.Substring(0, body.IndexOf("\""));
        
        
        while
          (player.PlaybackState == PlaybackState.Playing) await Task.Delay(1);


        var mf = new MediaFoundationReader(body);
        player = new WasapiOut();

        player.Init(mf);
        player.Play();
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
        public string text;
        //WaveIn waveIn;
        WaveFileWriter writer;
        public string outputFilename = "demo2.wav";
        string[] answer;
        private static Timer timer;
        public SpeechCap()
        {
            outputFilename = c++ + ".wav";

        }
        void waveIn_DataAvailable(
            object sender, WaveInEventArgs e)
        {
            writer.Write(e.Buffer, 0, e.BytesRecorded);

        }
        void waveIn_RecordingStopped(object sender, EventArgs e)
        {
            waveInEvent.Dispose();
            waveInEvent = null;
            //writer.Flush();       
            writer.Close();
            writer = null;

        }

        WaveInEvent waveInEvent;
        private static int c = 10;

        public void Listen()
        {
            waveInEvent = new WaveInEvent();
            waveInEvent.DeviceNumber = 0;
            waveInEvent.DataAvailable += waveIn_DataAvailable;
            waveInEvent
                .RecordingStopped += new EventHandler<StoppedEventArgs>(
                waveIn_RecordingStopped);
            waveInEvent.WaveFormat = new WaveFormat(16000, 1);
            writer = new WaveFileWriter(
                outputFilename, waveInEvent.WaveFormat);
            waveInEvent.StartRecording();
        }

        public void StopListen()
        {
            if (
                waveInEvent != null) waveInEvent.StopRecording();
            if (writer != null)
            {
                writer.Flush();
                writer.Dispose();

            }
            WorkWithAPI();

        }

        public void WorkWithAPI()
        {
            var client = new HttpClient();
            // client.        
            WebRequest request = WebRequest.Create("https://speech.googleapis.com/v1p1beta1/speech:recognize?key=AIzaSyBOti4mM-6x9WDnZIjIeyEU21OpBXqWBgw");
            //"https://www.google.com/speech-api/v2/recognize?output=json&lang=en-US&key=AIzaSyBOti4mM-6x9WDnZIjIeyEU21OpBXqWBgw");     
            request.Method = "POST";
            byte[] byteArray = null;
            while (byteArray == null)
            {
                try
                {
                    byteArray = File.ReadAllBytes(outputFilename);

                }
                catch (Exception e)
                {
                    Console.WriteLine("failed to read file");

                }

            }
            Console.WriteLine("read " + byteArray.Length + " bytes");
            request.ContentType = "audio/l16; rate=16000";
            //"16000";    
            request.ContentLength = byteArray.Length;
            request.GetRequestStream().Write(
                byteArray, 0, byteArray.Length);
            HttpWebResponse response = (HttpWebResponse)
                                                      request.GetResponse();
            StreamReader reader = new StreamReader(
                response.GetResponseStream());
            string freq = reader.ReadToEnd().ToLower();
            var speech = "";
            var speech2 = "";
            var answer = freq.Split('"'); for (int i = 0; i < answer.Length; i++)
            {
                if (answer[i] == ":") if (
                    speech == "") speech = answer[i + 1];
                    else if (speech2 == "")
                    {
                        speech2 = answer[i + 1];

                    }

            }
            Console.WriteLine(speech + freq);
            if (
                speech.Length > 0) Assistant2.text += speech;
            reader.Close();
            response.Close();

        }

    }


    public static class Assistant2
    {
        static public string text;
       // static WaveIn waveIn;
        static WaveFileWriter writer;
        static string outputFilename = "demo2.wav";
        static string[] answer;
        private static Timer timer;
        static WaveInEvent waveInEvent;
        private static List<SpeechCap> caps = new List<SpeechCap>();
        public static void Start()
        {
            timer = new Timer(on_timer, 0, 0, 3000);

        }
        private static bool re = false;
        private static void on_timer(object? state)
        {
            if (re) return;
            re = true;
            if (caps.Count > 0)
            {
                caps.Last().StopListen();
                System.Threading.Thread.Sleep(1000);

            }
            var cap = new SpeechCap();
            cap.Listen();
            caps.Add(cap);
            re = false;

        }

    }
}